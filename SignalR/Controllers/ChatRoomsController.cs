using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SignalR.Models;
using SignalR.Services;

namespace SignalR.Controllers
{
    [EnableCors("CORSPolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatRoomsController : ControllerBase
    {
        private readonly IChatRoomService _chatRoomService;

        public ChatRoomsController(IChatRoomService chatRoomService)
        {
            _chatRoomService = chatRoomService;
        }

        // GET: api/chatrooms
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var chatRooms = await _chatRoomService.GetChatRoomsAsync();

            return Ok(chatRooms);
        }

        // POST api/chatrooms
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatRoom chatRoom)
        {
            try {
                var result = await _chatRoomService.AddChatRoomAsync(chatRoom);

                return Ok(new { message = (!result ? "No se pudo crear la sala!" : "Sala creada!"), success = result });
            } catch(Exception ex) { 
                return BadRequest(new { message = "Error al crear la sala: " + ex.Message, success = false });
            }
        }
    }
}
