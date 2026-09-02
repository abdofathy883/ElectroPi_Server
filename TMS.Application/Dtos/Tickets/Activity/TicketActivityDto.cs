using ElectroPi.Domain.Enums;

namespace ElectroPi.Application.Dtos.Tickets.Activity
{
    public class TicketActivityDto
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public required string UserId { get; set; }
        public required string UserName { get; set; }
        public TicketActivityType Type { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
