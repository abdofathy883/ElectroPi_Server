using ElectroPi.Application.Dtos.Auth;
using ElectroPi.Application.Dtos.Password;

namespace ElectroPi.Application.Interfaces
{
    public interface IPasswordService
    {
        Task<UserDto> ChangePasswordAsync(ChangePasswordDto passwordDTO);
        //Task<string> RequestResetPasswordByAdminAsync(string userId);
        //Task ResetPasswordAsync(ResetPasswordDTO resetPasswordDTO);
    }
}
