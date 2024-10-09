using SignalR.Models;

namespace SignalR.Services
{
    public interface IMessageService
    {
        Task<List<Message>> GetMessagesAsync();
        Task<List<Message>> GetMessagesForChatRoomAsync(Guid roomId);
        Task<bool> AddMessageToRoomAsync(Guid roomId, Message message);
        Task<bool> AddSystemMessage(Message message);
        Task<bool> AddUserMessage(string roomName, Message message);
    }
}
