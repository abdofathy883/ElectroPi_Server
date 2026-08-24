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
        Task<UserDto> UpdateAsync(UpdateUserDto updatedUser);
        Task<bool> DeleteAsync(string userId);
        Task<List<LookupUsers>> LookupAsync();
    }
}
