using ElectroPi.Application.Dtos.Tickets.Reporting;

namespace ElectroPi.Application.Interfaces
{
    public interface ITicketReportingService
    {
        Task<TicketsReportDto> GetReportAsync();
    }
}
