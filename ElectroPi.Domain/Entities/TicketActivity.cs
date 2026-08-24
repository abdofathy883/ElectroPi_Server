using ElectroPi.Domain.Enums;

namespace ElectroPi.Domain.Entities
{
    public class TicketActivity
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = default!;
        public required string UserId { get; set; }
        public AppUser User { get; set; } = default!;
        public required string UserName { get; set; }
        public TicketActivityType Type { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
