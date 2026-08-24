namespace ElectroPi.Application.Dtos.Tickets.Time
{
    public class LogTimeEntryDto
    {
        public int TicketId { get; set; }
        public int DurationMinutes { get; set; }
        public required string Description { get; set; }
    }
}
