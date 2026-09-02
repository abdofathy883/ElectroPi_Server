namespace ElectroPi.Domain.Entities
{
    public class TimeEntry
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = default!;
        public required string AgentId { get; set; }
        public AppUser Agent { get; set; } = default!;
        public DateOnly WorkDate { get; set; }
        public int DurationMinutes { get; set; }
        public required string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
