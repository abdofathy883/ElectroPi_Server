using ElectroPi.Application.Interfaces;
using ElectroPi.Domain.Entities;
using ElectroPi.Domain.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Identity
{
    public class JwtService : IJwtServices
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly JwtOptions _jwtSettings;

        public JwtService(
            UserManager<AppUser> userManager, 
            JwtOptions jwtSettings
            )
        {
            _userManager = userManager;
            _jwtSettings = jwtSettings;
        }
        public async Task<string> GenerateAccessTokenAsync(AppUser appUser)
        {
            var userClaims = await _userManager.GetClaimsAsync(appUser);
            var userRoles = await _userManager.GetRolesAsync(appUser);
            var roleClaims = userRoles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();

            var claims = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new (JwtRegisteredClaimNames.Sub, appUser.Id),
                new (ClaimTypes.NameIdentifier, appUser.Id),
                new (ClaimTypes.Name, appUser.FullName),
                new (JwtRegisteredClaimNames.Email, appUser.Email ?? "")
            }.Union(userClaims)
                 .Union(roleClaims);

            var symetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));

            var signingCredentials = new SigningCredentials(symetricSecurityKey, SecurityAlgorithms.HmacSha256);
            var jwtSecurityToken = new JwtSecurityToken(
               issuer: _jwtSettings.Issuer,
               audience: _jwtSettings.Audience,
               claims: claims,
               expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes + 60),
               signingCredentials: signingCredentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            return accessToken;
        }

        public Task<RefreshToken> GenerateRefreshTokenAsync()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Task.FromResult(new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                CreateOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
            });
        }
    }
}
