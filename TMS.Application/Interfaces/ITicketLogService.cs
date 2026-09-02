using ElectroPi.Application.Dtos.Tickets.Time;

namespace ElectroPi.Application.Interfaces
{
    public interface ITicketLogService
    {
        Task<TimeEntryDto> LogAsync(LogTimeEntryDto request, string agentId);
        Task<List<TimeEntryDto>> GetAllByEmpIdAsync(string userId);
    }
}
