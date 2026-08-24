using ElectroPi.Domain.Enums;

namespace ElectroPi.Application.Dtos.Tickets
{
    public class CreateUpdateTicket
    {
        public required string Title { get; set; }
        public string? Description { get; set; }
        public TicketPriority Priority { get; set; }
        public TicketStatus Status { get; set; }
        public required string CustomerId { get; set; }
        public string? AgentId { get; set; }
    }
}
