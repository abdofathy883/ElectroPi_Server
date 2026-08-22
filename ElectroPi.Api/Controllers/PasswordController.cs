using ElectroPi.Application.Dtos.Password;
using ElectroPi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ElectroPi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PasswordController : ControllerBase
    {
        private readonly IPasswordService _passwordService;

        public PasswordController(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        [HttpPatch("set-password")]
        public async Task<IActionResult> ChangePasswordAsync(ChangePasswordDto changePasswordDTO)
        {
            if (changePasswordDTO is null)
                return BadRequest();

            if (changePasswordDTO.NewPassword != changePasswordDTO.ConfirmNewPassword)
                return BadRequest();

            try
            {
                var result = await _passwordService.ChangePasswordAsync(changePasswordDTO);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //[HttpPost("send-reset-link/{userId}")]
        //public async Task<IActionResult> SendResetLinkAsync(string userId)
        //{
        //    var link = await _passwordService.RequestResetPasswordByAdminAsync(userId);
        //    return Ok(new { link });
        //}

        //[AllowAnonymous]
        //[EnableRateLimiting("contact-limit")]
        //[HttpPost("reset-password")]
        //public async Task<IActionResult> ResetPasswordAsync(ResetPasswordDTO resetPasswordDTO)
        //{
        //    await _passwordService.ResetPasswordAsync(resetPasswordDTO);
        //    return Ok();
        //}
    }
}
