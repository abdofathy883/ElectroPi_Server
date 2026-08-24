namespace ElectroPi.Application.Dtos.Tickets.Comments
{
    public class CreateTicketCommentDto
    {
        public int TicketId { get; set; }
        public required string AuthorId { get; set; }
        public required string Content { get; set; }
    }
}
