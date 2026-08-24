using ElectroPi.Domain.Enums;

namespace ElectroPi.Domain.Entities
{
    public class Ticket
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public TicketPriority Priority { get; set; }
        public TicketStatus Status { get; set; }
        public required string CustomerId { get; set; }
        public AppUser Customer { get; set; } = default!;
        public string? AgentId { get; set; }
        public AppUser? Agent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public List<TicketComment> Comments { get; set; } = new();
        public List<TicketActivity> Activities { get; set; } = new();
        public List<TimeEntry> TimeEntries { get; set; } = new();
    }
}
