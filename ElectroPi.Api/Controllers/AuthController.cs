using ElectroPi.Application.Dtos.Auth;
using ElectroPi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ElectroPi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [EnableRateLimiting("contact-limit")]
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync(RegisterDto request)
        {
            if (request is null)
                return BadRequest("بيانات المستخدم غير صحيحة");
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsersAsync()
        {
            var result = await _authService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserByIdAsync(string userId)
        {
            try
            {
                var user = await _authService.GetByIdAsync(userId);
                return Ok(user);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("update-user")]
        public async Task<IActionResult> UpdateAsync(UpdateUserDto updateUserDTO)
        {
            if (updateUserDTO is null)
                return BadRequest();

            try
            {
                var result = await _authService.UpdateAsync(updateUserDTO);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest();

            try
            {
                var result = await _authService.DeleteAsync(userId);
                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        
    }
}
