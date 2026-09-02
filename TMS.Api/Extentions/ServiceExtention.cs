using ElectroPi.Application.Interfaces;
using ElectroPi.Infrastructure.Services.Tickets;
using Infrastructure.Identity;

namespace ElectroPi.Api.Extentions
{
    public static class ServiceExtention
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services
            )
        {
            services.AddAuthorization()
                .AddScoped<IAuthService, AuthService>()
                .AddScoped<IPasswordService, PasswordService>()
                .AddScoped<IJwtServices, JwtService>()
                .AddScoped<ITicketService, TicketService>()
                .AddScoped<ITicketLogService, TicketLogService>()
                .AddScoped<ITicketReportingService, TicketReportingService>()
                .AddScoped<TicketHelperService>()
                .AddHttpContextAccessor()
                .AddMemoryCache();

            return services;
        }
    }
}
