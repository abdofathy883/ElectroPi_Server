using ElectroPi.Application.Dtos.Tickets.Reporting;
using ElectroPi.Application.Interfaces;
using ElectroPi.Domain.Enums;
using ElectroPi.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace ElectroPi.Infrastructure.Services.Tickets
{
    public class TicketReportingService : ITicketReportingService
    {
        private readonly AppDbContext _dbContext;

        public TicketReportingService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<TicketsReportDto> GetReportAsync()
        {
            var tickets = await _dbContext.Tickets
                .AsQueryable()
                .AsNoTracking()
                .ToListAsync();

            var resolvedTickets = tickets
                .Where(t => t.ResolvedAt.HasValue)
                .ToList();

            var openCriticalTickets = tickets.Count(t =>
            t.Priority == TicketPriority.Critical &&
            (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress));

            var averageResolutionTime = 
                resolvedTickets.Count == 0 ? 0 : resolvedTickets.Average(t =>
                (t.ResolvedAt!.Value - t.CreatedAt).TotalHours);

            var agentWorkload = tickets
            .Where(t =>
                (t.Status == TicketStatus.Open ||
                 t.Status == TicketStatus.InProgress) &&
                t.AgentId != null)
            .GroupBy(t => new
            {
                t.AgentId,
                AgentName = t.Agent!.FullName
            })
            .Select(g => new AgentWorkloadDto
            {
                AgentName = g.Key.AgentName!,
                ActiveTicketsCount = g.Count()
            })
            .OrderByDescending(x => x.ActiveTicketsCount)
            .ToList();


            return new TicketsReportDto
            {
                TotalTickets = tickets.Count,
                OpenTickets = tickets.Count(t => t.Status == TicketStatus.Open),
                InProgressTickets = tickets.Count(t => t.Status == TicketStatus.InProgress),
                ResolvedTickets = tickets.Count(t => t.Status == TicketStatus.Resolved),
                ClosedTickets = tickets.Count(t => t.Status == TicketStatus.Closed),
                OpenCriticalTickets = openCriticalTickets,
                AverageResolutionTimeHours = averageResolutionTime,
                AgentWorkloads = agentWorkload
            };
        }
    }
}
