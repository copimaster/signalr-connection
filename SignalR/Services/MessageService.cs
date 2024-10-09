using Microsoft.EntityFrameworkCore;
using SignalR.Models;
using SignalR.Persistence;

namespace SignalR.Services
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Message>> GetMessagesAsync()
        {
            var messages = await _context.Messages.ToListAsync<Message>();

            return messages;
        }

        public async Task<List<Message>> GetMessagesForChatRoomAsync(Guid roomId)
        {
            var messagesForRoom = await _context.Messages.Where(m => m.RoomId == roomId).ToListAsync<Message>();

            return messagesForRoom;
        }

        public async Task<bool> AddMessageToRoomAsync(Guid roomId, Message message)
        {
            message.Id = Guid.NewGuid();
            message.RoomId = roomId;
            message.SentAt = DateTimeOffset.Now;

            _context.Messages.Add(message);

            var saveResults = await _context.SaveChangesAsync();

            return saveResults > 0;
        }

        public async Task<bool> AddSystemMessage(Message message)
        {
            message.Id = Guid.NewGuid();
            message.SentAt = DateTimeOffset.Now;
            message.Type = MessageType.System;

            _context.Messages.Add(message);

            var saveResults = await _context.SaveChangesAsync();

            return saveResults > 0;
        }

        public async Task<bool> AddUserMessage(string roomName, Message message)
        {
            var room = _context.ChatRooms.Where(x => x.Name == roomName).FirstOrDefault();

            message.Id = Guid.NewGuid();
            message.SentAt = DateTimeOffset.Now;
            message.RoomId = room?.Id;
            message.Type = MessageType.User;

            _context.Messages.Add(message);

            var saveResults = await _context.SaveChangesAsync();

            return saveResults > 0;
        }
    }
}
