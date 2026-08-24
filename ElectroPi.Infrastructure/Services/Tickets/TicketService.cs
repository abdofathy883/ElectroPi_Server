using AutoMapper;
using AutoMapper.QueryableExtensions;
using ElectroPi.Application.Dtos;
using ElectroPi.Application.Dtos.Tickets;
using ElectroPi.Application.Dtos.Tickets.Comments;
using ElectroPi.Application.Interfaces;
using ElectroPi.Domain.Entities;
using ElectroPi.Domain.Enums;
using ElectroPi.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace ElectroPi.Infrastructure.Services.Tickets
{
    public class TicketService : ITicketService
    {
        private readonly AppDbContext _dbContext;
        private readonly IAuthService _authService;
        private readonly TicketHelperService _helperService;
        private readonly IMapper _mapper;

        public TicketService(AppDbContext dbContext,
            IAuthService authService,
            TicketHelperService helperService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _authService = authService;
            _helperService = helperService;
            _mapper = mapper;
        }

        public async Task<bool> ChangeStatusAsync(int id, TicketStatus status, string currentUserId)
        {
            var ticket = await _dbContext.Tickets
                .Where(t => t.Id == id)
                .SingleAsync();

            var user = await _authService.GetByIdAsync(currentUserId);

            var oldStatus = ticket.Status;

            if (status == TicketStatus.Resolved)
                ticket.ResolvedAt = DateTime.UtcNow;
            
            if (status == TicketStatus.Closed)
                ticket.ClosedAt = DateTime.UtcNow;

            ticket.Status = status;
            await _helperService.CreateTicketActivity(ticket, currentUserId, TicketActivityType.StatusChanged, oldStatus.ToString(), status.ToString(), user.FullName);

            // Notify Customer

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<TicketDto> CreateAsync(CreateUpdateTicket request)
        {
            var user = await _authService.GetByIdAsync(request.CustomerId);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var ticket = new Ticket
                {
                    Title = request.Title,
                    Description = request.Description,
                    Priority = request.Priority,
                    Status = TicketStatus.Open,
                    CustomerId = request.CustomerId,
                };

                await _dbContext.Tickets.AddAsync(ticket);

                await _helperService.CreateTicketActivity(ticket, request.CustomerId, TicketActivityType.TicketCreation, "N/A", "N/A", user.FullName);

                await _dbContext.SaveChangesAsync();

                // Notification

                await transaction.CommitAsync();
                return _mapper.Map<TicketDto>(ticket);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<TicketCommentDto> CreateCommentAsync(CreateTicketCommentDto request)
        {
            var ticket = await _dbContext.Tickets
                .Where(t => t.Id == request.TicketId)
                .SingleAsync();

            var user = await _authService.GetByIdAsync(request.AuthorId);

            var newComment = new TicketComment
            {
                TicketId = request.TicketId,
                AuthorId = user.Id,
                Content = request.Content
            };

            await _dbContext.TicketComments.AddAsync(newComment);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<TicketCommentDto>(newComment);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var ticket = await _dbContext.Tickets
                .Where(t => t.Id == id)
                .SingleAsync();

            _dbContext.Tickets.Remove(ticket);

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<PagedResultsDto<TicketDto>> GetAllAsync(TicketFilterDto filter, string currentUserId)
        {
            var user = await _authService.GetByIdAsync(currentUserId);

            IQueryable<Ticket> query = _dbContext.Tickets
                .AsNoTracking();

            query = _helperService.FilterTasks(query, filter, user.Role, currentUserId);

            var totalRecords = await query.CountAsync();

            // Apply ordering and pagination
            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResultsDto<TicketDto>
            {
                Items = _mapper.Map<List<TicketDto>>(tickets),
                TotalCount = totalRecords,
                Page = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<TicketDto> GetByIdAsync(int id)
        {
            var task = await _dbContext.Tickets
                .AsNoTracking()
                .Where(t => t.Id == id)
                .ProjectTo<TicketDto>(_mapper.ConfigurationProvider)
                .SingleAsync();

            return task;
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

        public Task<TicketDto> UpdateAsync(CreateUpdateTicket request)
        {
            throw new NotImplementedException();
        }
    }
}
