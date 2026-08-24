using ElectroPi.Application.Dtos.Auth;

namespace ElectroPi.Application.Interfaces
{
    public interface IAuthService
    {
        Task<List<UserDto>> GetAllAsync();
        Task<UserDto> GetByIdAsync(string userId);
        Task<UserDto> RegisterAsync(RegisterDto newUser);
        Task<UserDto> RegisterCustomerAsync(PublicRegister newUser);
        Task<AuthResponseDto> LoginAsync(LoginDto login);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<UserDto> UpdateAsync(UpdateUserDto updatedUser, string currentUserId, string currentUserRole);
        Task<bool> DeleteAsync(string userId);
        Task<List<LookupUsers>> LookupAsync();
    }
}
