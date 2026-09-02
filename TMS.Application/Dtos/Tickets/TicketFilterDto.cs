using System.ComponentModel.DataAnnotations;
using ElectroPi.Domain.Enums;

namespace ElectroPi.Application.Dtos.Tickets
{
    public class TicketFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? AgentId { get; set; }
        public string? CustomerId { get; set; }

        [EnumDataType(typeof(TicketStatus))]
        public TicketStatus? Status { get; set; }

        [EnumDataType(typeof(TicketPriority))]
        public TicketPriority? Priority { get; set; }

        //public string? OnDeadline { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be at least 1.")]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize { get; set; } = 20;
    }
}
