using System.Text.Json.Serialization;

namespace ElectroPi.Application.Dtos.Auth
{
    public class AuthResponseDto
    {
        public string? Id { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Message { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? UserName { get; set; }
        public List<string>? Roles { get; set; }
        public string? Token { get; set; }
        [JsonIgnore]
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
    }
}
