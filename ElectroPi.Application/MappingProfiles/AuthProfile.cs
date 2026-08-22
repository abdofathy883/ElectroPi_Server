using AutoMapper;
using ElectroPi.Application.Dtos.Auth;
using ElectroPi.Domain.Entities;

namespace ElectroPi.Application.MappingProfiles
{
    public class AuthProfile : Profile
    {
        public AuthProfile()
        {
            CreateMap<AppUser, UserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber));
                //.ForMember(dest => dest.Roles, opt => opt.Map/From(src => src.Roles.SelectMany(r => r.Name)));
        }
    }
}
