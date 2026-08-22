using ElectroPi.Domain.Entities;

namespace ElectroPi.Application.Interfaces
{
    public interface IJwtServices
    {
        Task<string> GenerateAccessTokenAsync(AppUser appUser);
        Task<RefreshToken> GenerateRefreshTokenAsync();
    }
}
