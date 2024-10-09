using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace SignalR.Models
{
    public enum MessageType
    {
        User,      // Mensaje de usuario
        System     // Mensaje de sistema
    }

    public class Message
    {
        public Guid Id { get; set; } // Identificador único del mensaje

        [Required]
        public required string Content { get; set; } // Contenido del mensaje
        
        [AllowNull]
        public Guid? RoomId { get; set; } // ID de la sala (null si es un mensaje privado)

        [Required]
        public required string SenderId { get; set; } // ID del usuario que envía el mensaje
        public string? ReceiverId { get; set; } // ID del usuario que recibe el mensaje (null si es un mensaje de sala)
        public DateTimeOffset SentAt { get; set; } // Fecha y hora en que se envió el mensaje
        public MessageType Type { get; set; } // Tipo de mensaje (Usuario o Sistema)

        // Propiedades de navegación (opcional)
        [ForeignKey("RoomId")]
        public virtual ChatRoom? Room { get; set; } // Sala asociada al mensaje (opcional)
    }
}
