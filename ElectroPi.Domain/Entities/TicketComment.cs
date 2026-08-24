namespace ElectroPi.Domain.Entities
{
    public class TicketComment
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = default!;
        public required string AuthorId { get; set; }
        public AppUser Author { get; set; } = default!;
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
