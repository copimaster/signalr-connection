using System.ComponentModel.DataAnnotations;

namespace SignalR.Models
{
    public class ChatRoom
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
