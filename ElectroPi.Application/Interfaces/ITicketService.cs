using ElectroPi.Application.Dtos;
using ElectroPi.Application.Dtos.Tickets;
using ElectroPi.Application.Dtos.Tickets.Comments;
using ElectroPi.Domain.Enums;

namespace ElectroPi.Application.Interfaces
{
    public interface ITicketService
    {
        Task<PagedResultsDto<TicketDto>> GetAllAsync(TicketFilterDto filter, string currentUserId);
        Task<TicketDto> GetByIdAsync(int id, string currentUserId);
        Task<TicketDto> CreateAsync(CreateUpdateTicket request, string currentUserId);
        Task<TicketDto> UpdateAsync(int id, CreateUpdateTicket request, string currentUserId);
        Task<bool> ChangeStatusAsync(int id, TicketStatus status, string currentUserId);
        Task<List<TicketDto>> SearchAsync(string query, string currentUserId);
        Task<bool> DeleteAsync(int id);
        Task<TicketCommentDto> CreateCommentAsync(CreateTicketCommentDto request, string currentUserId);
    }
}
