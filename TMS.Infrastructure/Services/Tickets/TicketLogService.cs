using AutoMapper;
using AutoMapper.QueryableExtensions;
using ElectroPi.Application.Dtos.Tickets.Time;
using ElectroPi.Application.Interfaces;
using ElectroPi.Domain.Entities;
using ElectroPi.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ElectroPi.Infrastructure.Services.Tickets
{
    public class TicketLogService : ITicketLogService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TicketLogService> _logger;
        private readonly IMapper _mapper;

        public TicketLogService(AppDbContext dbContext, ILogger<TicketLogService> logger, IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<List<TimeEntryDto>> GetAllByEmpIdAsync(string userId)
        {
            var logs = await _dbContext.TimeEntries
                .AsNoTracking()
                .Where(l => l.AgentId == userId)
                .ProjectTo<TimeEntryDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return logs;
        }

        public async Task<TimeEntryDto> LogAsync(LogTimeEntryDto request, string agentId)
        {
            var log = new TimeEntry
            {
                TicketId = request.TicketId,
                AgentId = agentId,
                WorkDate = DateOnly.FromDateTime(DateTime.UtcNow),
                DurationMinutes = request.DurationMinutes,
                Description = request.Description
            };

            await _dbContext.TimeEntries.AddAsync(log);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Time entry {TimeEntryId} logged for ticket {TicketId} by agent {AgentId} ({DurationMinutes} minutes)", log.Id, request.TicketId, agentId, request.DurationMinutes);
            return _mapper.Map<TimeEntryDto>(log);
        }
    }
}
