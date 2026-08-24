using System.ComponentModel.DataAnnotations;

namespace ElectroPi.Application.Dtos.Tickets.Time
{
    public class LogTimeEntryDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid TicketId is required.")]
        public int TicketId { get; set; }

        [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes.")]
        public int DurationMinutes { get; set; }

        [Required, StringLength(1000, MinimumLength = 1)]
        public required string Description { get; set; }
    }
}
