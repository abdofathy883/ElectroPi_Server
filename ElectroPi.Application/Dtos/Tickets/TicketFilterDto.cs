using ElectroPi.Domain.Enums;

namespace ElectroPi.Application.Dtos.Tickets
{
    public class TicketFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? AgentId { get; set; }
        public string? CustomerId { get; set; }
        public TicketStatus? Status { get; set; }
        public TicketPriority? Priority { get; set; }
        //public string? OnDeadline { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
