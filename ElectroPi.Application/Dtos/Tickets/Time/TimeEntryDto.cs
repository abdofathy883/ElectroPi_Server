namespace ElectroPi.Application.Dtos.Tickets.Time
{
    public class TimeEntryDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public required string AgentId { get; set; }
        public required string AgentName { get; set; }
        public DateOnly WorkDate { get; set; }
        public int DurationMinutes { get; set; }
        public required string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
