using ElectroPi.Application.Dtos.Tickets;
using ElectroPi.Domain.Entities;
using ElectroPi.Domain.Enums;
using ElectroPi.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace ElectroPi.Infrastructure.Services.Tickets
{
    public class TicketHelperService
    {
        private readonly AppDbContext _dbContext;

        public TicketHelperService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
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
