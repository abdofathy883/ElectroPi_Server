using System.ComponentModel.DataAnnotations;
using ElectroPi.Domain.Enums;

namespace ElectroPi.Application.Dtos.Tickets
{
    public class CreateUpdateTicket
    {
        [Required, StringLength(200, MinimumLength = 3)]
        public required string Title { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [EnumDataType(typeof(TicketPriority))]
        public TicketPriority Priority { get; set; }

        [EnumDataType(typeof(TicketStatus))]
        public TicketStatus Status { get; set; }

        [Required]
        public required string CustomerId { get; set; }

        public string? AgentId { get; set; }
    }
}
