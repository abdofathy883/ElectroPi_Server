using AutoMapper;
using ElectroPi.Application.Dtos.Tickets;
using ElectroPi.Application.Dtos.Tickets.Activity;
using ElectroPi.Application.Dtos.Tickets.Comments;
using ElectroPi.Application.Dtos.Tickets.Time;
using ElectroPi.Domain.Entities;

namespace ElectroPi.Application.MappingProfiles
{
    public class TicketProfile : Profile
    {
        public TicketProfile()
        {
            CreateMap<Ticket, TicketDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.FullName))
                .ForMember(dest => dest.AgentName, opt => opt.MapFrom(src => src.Agent.FullName))
                .ForMember(dest => dest.TicketActivities, opt => opt.MapFrom(src => src.Activities));

            CreateMap<TicketComment, TicketCommentDto>();

            CreateMap<TicketActivity, TicketActivityDto>();

            CreateMap<TimeEntry, TimeEntryDto>();
        }
    }
}
