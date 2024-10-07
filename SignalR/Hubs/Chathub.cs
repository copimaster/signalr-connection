using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalR.Models;
using SignalR.Services;
using System.Security.Claims;

namespace SignalR.Hubs
{
    public interface IChatClient
    {
        /// <summary>
        /// Notifica a todos los usuarios en la sala que un nuevo miembro se ha unido.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task UserJoinedGroup(string userName, string message);

        /// <summary>
        /// Confirma al usuario que su intento de unirse a la sala fue exitoso.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task JoinedGroup(string userName, string message);

        /// <summary>
        /// Enviamos una confirmación a todos los clientes que otro dejó la sala.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task UserLeftGroup(string message);

        /// <summary>
        /// Enviamos una confirmación al cliente de que dejó la sala.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task LeftGroup(string message);

        /// <summary>
        /// Notificamos a todos los grupos a los que el usuario se había unido que el usuario se ha desconectado.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task DisconnectedUser(string userName, string message);
        public Task ReceivedMessage(string userName, string message);
        public Task ReceivePrivateMessage(string? userId, string username, string message);

        // Recibe el nombre del usuario que está escribiendo y lo envía a los demás clientes conectados.
        public Task ReceiveTypingGroup(string userName);
        public Task ReceiveStopTypingGroup(string userName);

        public Task ReceivePrivateTyping(string userName);
        public Task ReceivePrivateStopTyping(string userName);

        public Task AddedGroup(string roomName, Guid roomId);

        /// <summary>
        /// Notify remaining users in the Group about the updated user list.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public Task UserList(string roomName, IEnumerable<string> list);
    }

    public class Chathub : Hub<IChatClient> //: Hub use .sendAsync('event', params)
    {
        private readonly SharedDb _sharedDb;
        private readonly IChatRoomService _chatRoomService;
        private readonly IMessageService _messageService;
        public int UsersOnline;

        public Chathub(SharedDb sharedDb, IChatRoomService chatRoomService, IMessageService messageService)
        {
            _sharedDb = sharedDb;
            _chatRoomService = chatRoomService;
            _messageService = messageService;
        }

        public async Task JoinChat(UserConnection conn)
        {
            await Clients.All.UserJoinedGroup("admin", $"{conn.UserName} has Joined.");
        }

        public async Task JoinGroup(UserConnection conn)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conn.ChatRoom);
            conn.UserId = Context.UserIdentifier!;

            _sharedDb.Connections[Context.ConnectionId] = conn;

            // Mantiene un registro de las salas por usuario.
            _sharedDb.UserGroups.AddOrUpdate(Context.ConnectionId, [conn.ChatRoom], (key, oldSet) => {
                oldSet.Add(conn.ChatRoom);
                return oldSet;
            });

            _sharedDb.GroupUsers.AddOrUpdate(conn.ChatRoom, [Context.UserIdentifier], (key, oldSet) => {
                oldSet.Add(Context.UserIdentifier!);
                return oldSet;
            });

            await Clients.Group(conn.ChatRoom).UserJoinedGroup(conn.UserName, $"{conn.UserName} has Joined {conn.ChatRoom}");
            await Clients.Caller.JoinedGroup(conn.UserName, $"Welcome {conn.UserName} to Group Chat!");

            // Notify the caller about all users in the room
            await Clients.Caller.UserList(conn.ChatRoom, GetUsersInRoom(conn.ChatRoom));
        }

        public async Task LeaveGroup(UserConnection conn)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conn.ChatRoom);

            if (_sharedDb.UserGroups.TryGetValue(Context.ConnectionId, out var rooms)) {
                // Removemos la sala del conjunto de salas del usuario
                rooms.Remove(conn.ChatRoom);
            }

            if (_sharedDb.GroupUsers.TryGetValue(conn.ChatRoom, out var users))
            {
                users.Remove(Context.UserIdentifier!);
                // Si la sala ya no tiene una lista de usuarios conectados lo eliminamos
                if (users.Count == 0) {
                    _sharedDb.GroupUsers.TryRemove(conn.ChatRoom, out _);
                }
            }

            await Clients.Group(conn.ChatRoom).UserLeftGroup($"{conn.UserName} salió del canal");
            await Clients.Caller.LeftGroup("Regresa pronto!");
            await Clients.Group(conn.ChatRoom).UserList(conn.ChatRoom, GetUsersInRoom(conn.ChatRoom));
        }

        public async Task SendMessage(string msg)
        {
            if (_sharedDb.Connections.TryGetValue(Context.ConnectionId, out UserConnection? conn))
            {
                await Clients.Group(conn.ChatRoom).ReceivedMessage(conn.UserName, msg);
            }
        }

        public async Task SendPrivateMessage(string receiverId, string username, string message)
        {
            await Clients.User(receiverId).ReceivePrivateMessage(Context.UserIdentifier, username, message);
        }

        public async Task SendTypingInGroup(string groupName, string user)
        {
            //await Clients.Others.ReceiveTypingNotification(user);
            await Clients.OthersInGroup(groupName).ReceiveTypingGroup(user);
        }

        public async Task SendStopTypingInGroup(string groupName, string user)
        {
            await Clients.OthersInGroup(groupName).ReceiveStopTypingGroup(user);
        }

        public async Task SendPrivateTyping(string connectionId)
        {
            var httpContext = Context.GetHttpContext();
            var userName = httpContext?.User.FindFirst(ClaimTypes.Name)?.Value;

            await Clients.Client(connectionId).ReceivePrivateTyping(userName!);
        }

        public async Task SendStopPrivateTyping(string connectionId)
        {
            string senderUserId = Context?.User?.Identity?.Name!;
            await Clients.Client(connectionId).ReceivePrivateStopTyping(senderUserId);
        }

        public async Task SendMessageAsync(Guid roomId, string user, string message)
        {
            Message m = new() {
                RoomId = roomId,
                Contents = message,
                UserName = user
            };

            await _messageService.AddMessageToRoomAsync(roomId, m);
            //await Clients.All.ReceiveMessage(user, message, roomId, m.Id, m.PostedAt);
        }

        public async Task AddChatRoom(string roomName)
        {
            ChatRoom chatRoom = new() {
                Name = roomName
            };

            await _chatRoomService.AddChatRoomAsync(chatRoom);
            await Clients.All.AddedGroup(roomName, chatRoom.Id);
        }

        public override async Task OnConnectedAsync()
        {
            UsersOnline++;
            _sharedDb.UserGroups[Context.ConnectionId] = [];

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            UsersOnline--;
            // Check if the disconnected user is in the dictionary
            if (_sharedDb.UserGroups.TryGetValue(Context.ConnectionId, out var groups))
            {
                string username = Context.ConnectionId;
                if (_sharedDb.Connections.TryGetValue(Context.ConnectionId, out UserConnection? conn))
                {
                    username = conn.UserName;
                }

                // Notificar a los usuarios del mismo grupo (sala)
                foreach (var groupName in groups)
                {
                    // Removemos al usuario de todas las salas a las que se había unido.
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                    if (_sharedDb.GroupUsers.TryGetValue(groupName, out var users))
                    {
                        users.Remove(Context.UserIdentifier!);

                        if (users.Count == 0) {
                            _sharedDb.GroupUsers.TryRemove(groupName, out _);
                        } else {
                            await Clients.Group(groupName).UserList(groupName, GetUsersInRoom(groupName));
                        }
                    }

                    await Clients.Group(groupName).DisconnectedUser(username, $"El usuario '{username}' salió del grupo '{groupName}'");
                }
            }

            _sharedDb.Connections.TryRemove(Context.ConnectionId, out _);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Retrieves the groups associated with the current client connection. All connections and groups of other clients are not retrieved.
        /// 
        /// <para>Note: This is because the 'Context.ConnectionId' is related only to the current connection making the request.</para>
        /// </summary>
        /// <returns></returns>
        public List<string> GetGroups()
        {
            if (_sharedDb.UserGroups.TryGetValue(Context.ConnectionId, out var groups))
            {
                return [.. groups];
            }

            return [];
        }

        public List<KeyValuePair<string, UserConnection>> GetConnectedUsers()
        {
            // Obtiene la lista de nombres de usuario conectados
            //List<string> connectedUsers = _sharedDb.Connections.Values.Select(c => c.UserName).Distinct().ToList();
            var connectedUsers = _sharedDb.Connections.ToList();
            return connectedUsers;
        }

        public IEnumerable<string> GetUsersInRoom(string roomName)
        {
            if (_sharedDb.GroupUsers.TryGetValue(roomName, out var users)) {
                return users;
            }

            return [];
        }

        public List<KeyValuePair<string, HashSet<string>>> GetAllUsersByRoom()
        {
            var list = _sharedDb.GroupUsers.ToList();
            return list;
        }
    }
}
