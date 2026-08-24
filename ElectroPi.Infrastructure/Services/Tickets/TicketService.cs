using AutoMapper;
using AutoMapper.QueryableExtensions;
using ElectroPi.Application.Dtos;
using ElectroPi.Application.Dtos.Tickets;
using ElectroPi.Application.Dtos.Tickets.Comments;
using ElectroPi.Application.Interfaces;
using ElectroPi.Domain.Entities;
using ElectroPi.Domain.Enums;
using ElectroPi.Domain.Exceptions;
using ElectroPi.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ElectroPi.Infrastructure.Services.Tickets
{
    public class TicketService : ITicketService
    {
        private readonly AppDbContext _dbContext;
        private readonly IAuthService _authService;
        private readonly TicketHelperService _helperService;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<TicketService> _logger;
        private readonly IMapper _mapper;

        private string CacheKey = "tickets_lookup";

        // Only these transitions are allowed for non-Admin roles. Admins may force any status.
        private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedStatusTransitions = new()
        {
            [TicketStatus.Open] = new[] { TicketStatus.Acknowledged, TicketStatus.InProgress },
            [TicketStatus.Acknowledged] = new[] { TicketStatus.InProgress },
            [TicketStatus.InProgress] = new[] { TicketStatus.Resolved },
            [TicketStatus.Resolved] = new[] { TicketStatus.Closed, TicketStatus.InProgress },
            [TicketStatus.Closed] = Array.Empty<TicketStatus>(),
        };

        private static void EnsureTicketAccess(string role, string currentUserId, string customerId, string? agentId)
        {
            if (role == UserRole.Admin.ToString())
                return;

            if (role == UserRole.Agent.ToString() && agentId == currentUserId)
                return;

            if (role == UserRole.Customer.ToString() && customerId == currentUserId)
                return;

            throw new ForbiddenException("You do not have access to this ticket.");
        }

        public TicketService(AppDbContext dbContext,
            IAuthService authService,
            TicketHelperService helperService,
            IMemoryCache memoryCache,
            ILogger<TicketService> logger,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _authService = authService;
            _helperService = helperService;
            _memoryCache = memoryCache;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<bool> ChangeStatusAsync(int id, TicketStatus status, string currentUserId)
        {
            var ticket = await _dbContext.Tickets
                .Where(t => t.Id == id)
                .SingleOrDefaultAsync()
                ?? throw new NotFoundException("Ticket not found.");

            var user = await _authService.GetByIdAsync(currentUserId);

            EnsureTicketAccess(user.Role, currentUserId, ticket.CustomerId, ticket.AgentId);

            var oldStatus = ticket.Status;

            if (oldStatus == status)
                return true;

            if (user.Role == UserRole.Customer.ToString())
            {
                if (!(oldStatus == TicketStatus.Resolved && status == TicketStatus.Closed))
                    throw new ForbiddenException("Customers may only close a resolved ticket.");
            }
            else if (user.Role != UserRole.Admin.ToString())
            {
                if (!AllowedStatusTransitions.TryGetValue(oldStatus, out var allowed) || !allowed.Contains(status))
                    throw new InvalidOperationException($"Cannot change ticket status from {oldStatus} to {status}.");
            }

            if (status == TicketStatus.Resolved)
                ticket.ResolvedAt = DateTime.UtcNow;
            
            if (status == TicketStatus.Closed)
                ticket.ClosedAt = DateTime.UtcNow;

            ticket.Status = status;
            await _helperService.CreateTicketActivity(ticket, currentUserId, TicketActivityType.StatusChanged, oldStatus.ToString(), status.ToString(), user.FullName);

            // Notify Customer

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Ticket {TicketId} status changed from {OldStatus} to {NewStatus} by user {UserId}", id, oldStatus, status, currentUserId);
            return true;
        }

        public async Task<TicketDto> CreateAsync(CreateUpdateTicket request, string currentUserId)
        {
            var currentUser = await _authService.GetByIdAsync(currentUserId);

            // Customers may only ever create tickets for themselves, regardless of what CustomerId is submitted.
            var customerId = currentUser.Role == UserRole.Customer.ToString()
                ? currentUserId
                : request.CustomerId;

            var customer = await _authService.GetByIdAsync(customerId);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var ticket = new Ticket
                {
                    Title = request.Title,
                    Description = request.Description,
                    Priority = request.Priority,
                    Status = TicketStatus.Open,
                    CustomerId = customerId,
                };

                await _dbContext.Tickets.AddAsync(ticket);

                await _helperService.CreateTicketActivity(ticket, currentUserId, TicketActivityType.TicketCreation, "N/A", "N/A", customer.FullName);

                await _dbContext.SaveChangesAsync();

                // Notification

                await transaction.CommitAsync();
                _logger.LogInformation("Ticket {TicketId} created for customer {CustomerId} by user {CurrentUserId}", ticket.Id, customerId, currentUserId);
                return _mapper.Map<TicketDto>(ticket);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create ticket for customer {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<TicketCommentDto> CreateCommentAsync(CreateTicketCommentDto request, string currentUserId)
        {
            var ticket = await _dbContext.Tickets
                .Where(t => t.Id == request.TicketId)
                .SingleOrDefaultAsync()
                ?? throw new NotFoundException("Ticket not found.");

            var user = await _authService.GetByIdAsync(currentUserId);

            EnsureTicketAccess(user.Role, currentUserId, ticket.CustomerId, ticket.AgentId);

            // AuthorId always comes from the authenticated caller, never from the request body.
            var newComment = new TicketComment
            {
                TicketId = request.TicketId,
                AuthorId = currentUserId,
                Content = request.Content
            };

            await _dbContext.TicketComments.AddAsync(newComment);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Comment {CommentId} added to ticket {TicketId} by user {AuthorId}", newComment.Id, request.TicketId, currentUserId);
            return _mapper.Map<TicketCommentDto>(newComment);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var ticket = await _dbContext.Tickets
                .Where(t => t.Id == id)
                .SingleAsync();

            _dbContext.Tickets.Remove(ticket);

            var deleted = await _dbContext.SaveChangesAsync() > 0;
            _logger.LogInformation("Ticket {TicketId} deleted", id);
            return deleted;
        }

        public async Task<PagedResultsDto<TicketDto>> GetAllAsync(TicketFilterDto filter, string currentUserId)
        {
            var user = await _authService.GetByIdAsync(currentUserId);

            IQueryable<Ticket> query = _dbContext.Tickets
                .AsNoTracking();

            query = _helperService.FilterTasks(query, filter, user.Role, currentUserId);

            var totalRecords = await query.CountAsync();

            query = ApplySorting(query, filter.SortBy, filter.SortDescending);

            // Apply ordering and pagination
            var tickets = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ProjectTo<TicketDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new PagedResultsDto<TicketDto>
            {
                Items = tickets,
                TotalCount = totalRecords,
                Page = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<TicketDto> GetByIdAsync(int id, string currentUserId)
        {
            var ticket = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.Id == id)
                .ProjectTo<TicketDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync()
                ?? throw new NotFoundException("Ticket not found.");

            var user = await _authService.GetByIdAsync(currentUserId);
            EnsureTicketAccess(user.Role, currentUserId, ticket.CustomerId, ticket.AgentId);

            return ticket;
        }

        private static IQueryable<Ticket> ApplySorting(IQueryable<Ticket> query, string? sortBy, bool descending)
        {
            return (sortBy?.Trim().ToLowerInvariant(), descending) switch
            {
                ("title", true) => query.OrderByDescending(t => t.Title),
                ("title", false) => query.OrderBy(t => t.Title),
                ("status", true) => query.OrderByDescending(t => t.Status),
                ("status", false) => query.OrderBy(t => t.Status),
                ("priority", true) => query.OrderByDescending(t => t.Priority),
                ("priority", false) => query.OrderBy(t => t.Priority),
                ("customer", true) => query.OrderByDescending(t => t.Customer.FullName),
                ("customer", false) => query.OrderBy(t => t.Customer.FullName),
                ("agent", true) => query.OrderByDescending(t => t.Agent!.FullName),
                ("agent", false) => query.OrderBy(t => t.Agent!.FullName),
                (_, true) => query.OrderByDescending(t => t.CreatedAt),
                (_, false) => query.OrderBy(t => t.CreatedAt),
            };
        }

        public async Task<List<TicketDto>> SearchAsync(string query, string currentUserId)
        {
            var user = await _authService.GetByIdAsync(currentUserId);

            IQueryable<Ticket> ticketsQuery = _dbContext.Tickets;

            if (user.Role == UserRole.Admin.ToString())
            {
                // no extra filter
            }
            else if (user.Role == UserRole.Agent.ToString())
            {
                ticketsQuery = ticketsQuery.Where(t => t.AgentId == currentUserId);
            }
            else if (user.Role == UserRole.Customer.ToString())
            {
                ticketsQuery = ticketsQuery.Where(t => t.CustomerId == currentUserId);
            }

            ticketsQuery = ticketsQuery.Where(t =>
                EF.Functions.Like(t.Title, $"%{query}%"));

            var tickets = await ticketsQuery
                .OrderByDescending(t => t.Id)
                .ToListAsync();

            return _mapper.Map<List<TicketDto>>(tickets);
        }

        public async Task<TicketDto> UpdateAsync(int id, CreateUpdateTicket request, string currentUserId)
        {
            var ticket = await _dbContext.Tickets
                .Where(t => t.Id == id)
                .SingleOrDefaultAsync()
                ?? throw new NotFoundException("Ticket not found.");

            var user = await _authService.GetByIdAsync(currentUserId);

            EnsureTicketAccess(user.Role, currentUserId, ticket.CustomerId, ticket.AgentId);

            // Customers cannot edit ticket details; status is managed exclusively via ChangeStatusAsync,
            // and ownership (CustomerId) is immutable once a ticket is created.
            if (user.Role == UserRole.Customer.ToString())
                throw new ForbiddenException("Customers may not edit ticket details.");

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                ticket.Title = request.Title;
                ticket.Description = request.Description;

                if (ticket.Priority != request.Priority)
                {
                    var oldPriority = ticket.Priority;
                    ticket.Priority = request.Priority;
                    await _helperService.CreateTicketActivity(ticket, currentUserId, TicketActivityType.PriorityChanged, oldPriority.ToString(), request.Priority.ToString(), user.FullName);
                }

                if (user.Role == UserRole.Admin.ToString() && ticket.AgentId != request.AgentId)
                {
                    var oldAgentId = ticket.AgentId ?? "N/A";

                    if (!string.IsNullOrEmpty(request.AgentId))
                    {
                        // Validates the new agent exists before assigning.
                        await _authService.GetByIdAsync(request.AgentId);
                        ticket.AgentId = request.AgentId;
                        await _helperService.CreateTicketActivity(ticket, currentUserId, TicketActivityType.AgentAssigned, oldAgentId, request.AgentId, user.FullName);
                    }
                    else
                    {
                        ticket.AgentId = null;
                        await _helperService.CreateTicketActivity(ticket, currentUserId, TicketActivityType.AgentUnassigned, oldAgentId, "N/A", user.FullName);
                    }
                }

                ticket.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Ticket {TicketId} updated by user {UserId}", id, currentUserId);
                return _mapper.Map<TicketDto>(ticket);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to update ticket {TicketId}", id);
                throw;
            }
        }
    }
}
