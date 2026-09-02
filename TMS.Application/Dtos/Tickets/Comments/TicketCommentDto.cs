namespace ElectroPi.Application.Dtos.Tickets.Comments
{
    public class TicketCommentDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public required string AuthorId { get; set; }
        public required string AuthorName { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
