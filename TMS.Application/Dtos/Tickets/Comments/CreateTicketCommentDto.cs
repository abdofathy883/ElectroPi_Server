using System.ComponentModel.DataAnnotations;

namespace ElectroPi.Application.Dtos.Tickets.Comments
{
    public class CreateTicketCommentDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid TicketId is required.")]
        public int TicketId { get; set; }

        public required string AuthorId { get; set; }

        [Required, StringLength(2000, MinimumLength = 1)]
        public required string Content { get; set; }
    }
}
