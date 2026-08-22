using Microsoft.AspNetCore.Identity;

namespace ElectroPi.Domain.Entities
{
    public class AppUser : IdentityUser
    {
        public required string FullName { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
