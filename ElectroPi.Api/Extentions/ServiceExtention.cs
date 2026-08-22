using ElectroPi.Application.Interfaces;
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
                .AddHttpContextAccessor()
                .AddMemoryCache();

            return services;
        }
    }
}
