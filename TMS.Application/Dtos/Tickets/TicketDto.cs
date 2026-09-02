using ElectroPi.Application.Dtos.Tickets.Activity;
using ElectroPi.Application.Dtos.Tickets.Comments;
using ElectroPi.Application.Dtos.Tickets.Time;
using ElectroPi.Domain.Enums;

namespace ElectroPi.Application.Dtos.Tickets
{
    public class TicketDto
    {
        public int Id { get; set; }
        public required string TicketNumber { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public TicketPriority Priority { get; set; }
        public TicketStatus Status { get; set; }
        public required string CustomerId { get; set; }
        public required string CustomerName { get; set; }
        public string? AgentId { get; set; }
        public string? AgentName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public List<TicketCommentDto> Comments { get; set; } = new();
        public List<TicketActivityDto> TicketActivities { get; set; } = new();
        public List<TimeEntryDto> TimeEntries { get; set; } = new();
    }
}
