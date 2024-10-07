using Microsoft.EntityFrameworkCore;
using SignalR.Models;
using SignalR.Persistence;

namespace SignalR.Services
{
    public class ChatRoomService : IChatRoomService
    {
        private readonly ApplicationDbContext _context;

        public ChatRoomService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChatRoom>> GetChatRoomsAsync()
        {
            var chatRooms = await _context.ChatRooms.ToListAsync<ChatRoom>();

            return chatRooms;
        }

        /// <summary>
        /// Verificar si ya existe una sala con el mismo nombre (ignorando mayúsculas), y si no existe la crea.
        /// <para>Nota: No usar 'StringComparison.CurrentCultureIgnoreCase(string, StringComparison.CurrentCultureIgnoreCase)' para ignorar mayusculas o minusculas, ya que Entity Framework no puede traducir la comparación directamente a SQL. Esto sucede porque SQL no admite esa opción de comparación de forma nativa.</para>
        /// </summary>
        /// <param name="chatRoom"></param>
        /// <returns></returns>
        public async Task<bool> AddChatRoomAsync(ChatRoom chatRoom)
        {
            var existingRoom = await _context.ChatRooms.FirstOrDefaultAsync(room => room.Name.ToLower() == chatRoom.Name.ToLower());

            if (existingRoom != null)
            {
                // Si existe, no permitir la creación de la sala
                return false;
            }

            // Si no existe, crear una nueva sala
            chatRoom.Id = Guid.NewGuid();
            _context.ChatRooms.Add(chatRoom);

            var saveResults = await _context.SaveChangesAsync();

            return saveResults > 0;
        }
    }
}
