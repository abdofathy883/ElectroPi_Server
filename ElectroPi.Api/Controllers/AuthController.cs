using ElectroPi.Application.Dtos.Auth;
using ElectroPi.Application.Interfaces;
using ElectroPi.Domain.Enums;
using ElectroPi.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ElectroPi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("fixed")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private const string RefreshTokenCookieName = "refreshToken";

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        private void SetRefreshTokenCookie(string token, DateTime expires)
        {
            Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expires,
                Path = "/api/auth"
            });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginDto request)
        {
            var result = await _authService.LoginAsync(request);
            SetRefreshTokenCookie(result.RefreshToken!, result.RefreshTokenExpiration);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshTokenAsync()
        {
            var refreshToken = Request.Cookies[RefreshTokenCookieName];
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new UnauthorizedAccessException("Refresh token was not found.");

            var result = await _authService.RefreshTokenAsync(refreshToken);
            SetRefreshTokenCookie(result.RefreshToken!, result.RefreshTokenExpiration);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeTokenAsync()
        {
            var refreshToken = Request.Cookies[RefreshTokenCookieName];
            if (string.IsNullOrWhiteSpace(refreshToken))
                return BadRequest("Refresh token was not found.");

            await _authService.RevokeTokenAsync(refreshToken);
            Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/api/auth" });
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> RegisterAsync(PublicRegister request)
        {
            if (request is null)
                return BadRequest("بيانات المستخدم غير صحيحة");

            var result = await _authService.RegisterCustomerAsync(request);
            return Ok(result);
        }

        [HttpPost("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterAsync(RegisterDto request)
        {
            if (request is null)
                return BadRequest("Invalid User's data");

            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsersAsync()
        {
            var result = await _authService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserByIdAsync(string userId)
        {
            if (userId != GetUserId() && GetUserRole() != UserRole.Admin.ToString())
                throw new ForbiddenException("You can only view your own account.");

            var user = await _authService.GetByIdAsync(userId);
            return Ok(user);
        }

        [HttpPatch("update-user")]
        public async Task<IActionResult> UpdateAsync(UpdateUserDto updateUserDTO)
        {
            if (updateUserDTO is null)
                return BadRequest();

            var result = await _authService.UpdateAsync(updateUserDTO, GetUserId(), GetUserRole());
            return Ok(result);
        }

        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest();

            var result = await _authService.DeleteAsync(userId);
            return Ok(result);
        }

        [HttpGet("lookup")]
        //[Route]
        public async Task<IActionResult> Lookup()
        {
            var result = await _authService.LookupAsync();
            return Ok(result);
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");

        private string GetUserRole() =>
            User.FindFirstValue(ClaimTypes.Role)
            ?? throw new UnauthorizedAccessException("Role not found in token.");
    }
}
