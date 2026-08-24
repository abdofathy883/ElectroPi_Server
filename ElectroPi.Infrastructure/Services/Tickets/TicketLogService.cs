using AutoMapper;
using AutoMapper.QueryableExtensions;
using ElectroPi.Application.Dtos.Tickets.Time;
using ElectroPi.Application.Interfaces;
using ElectroPi.Domain.Entities;
using ElectroPi.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace ElectroPi.Infrastructure.Services.Tickets
{
    public class TicketLogService : ITicketLogService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public TicketLogService(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
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
            return _mapper.Map<TimeEntryDto>(log);
        }
    }
}
