using ElectroPi.Application.Dtos.Password;
using ElectroPi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ElectroPi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("fixed")]
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

            var result = await _passwordService.ChangePasswordAsync(changePasswordDTO);
            return Ok(result);
        }
    }
}
