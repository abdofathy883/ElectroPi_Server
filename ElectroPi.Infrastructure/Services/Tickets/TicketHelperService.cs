using ElectroPi.Application.Dtos.Tickets;
using ElectroPi.Domain.Entities;
using ElectroPi.Domain.Enums;
using ElectroPi.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ElectroPi.Infrastructure.Services.Tickets
{
    public class TicketHelperService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TicketHelperService> _logger;

        public TicketHelperService(AppDbContext dbContext, ILogger<TicketHelperService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task CreateTicketActivity(Ticket ticket, string userId, TicketActivityType type, string oldValue, string newValue, string userName)
        {
            var activity = new TicketActivity
            {
                Ticket = ticket,
                UserId = userId,
                Type = type,
                OldValue = oldValue,
                NewValue = newValue,
                UserName = userName,
            };
            ticket.Activities.Add(activity);
            await _dbContext.TicketActivities.AddAsync(activity);
            _logger.LogInformation("Ticket activity {ActivityType} recorded for ticket {TicketId} by user {UserId}", type, ticket.Id, userId);
        }

        public IQueryable<Ticket> FilterTasks(IQueryable<Ticket> query, TicketFilterDto filter, string role, string currentUserId)
        {
            if (role == UserRole.Admin.ToString())
            {
                // Return all tickets (no userId filter)
            }
            else if (role == UserRole.Agent.ToString())
            {
                query = query.Where(t => t.AgentId == currentUserId);
            }
            else if (role == UserRole.Customer.ToString())
            {
                query = query.Where(t => t.CustomerId == currentUserId);
            }

            if (filter.FromDate.HasValue)
                query = query.Where(t => t.CreatedAt.Date >= filter.FromDate.Value.Date);

            if (filter.ToDate.HasValue)
                query = query.Where(t => t.CreatedAt.Date <= filter.ToDate.Value.Date);

            if (!string.IsNullOrEmpty(filter.AgentId))
                query = query.Where(t => t.AgentId == filter.AgentId);

            if (!string.IsNullOrEmpty(filter.CustomerId))
                query = query.Where(t => t.CustomerId == filter.CustomerId);

            if (filter.Status.HasValue)
                query = query.Where(t => t.Status == filter.Status);

            if (filter.Priority.HasValue)
                query = query.Where(t => t.Priority == filter.Priority);

            return query;
        }
    }
}
