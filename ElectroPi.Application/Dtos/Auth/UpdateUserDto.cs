using ElectroPi.Domain.Enums;

namespace ElectroPi.Application.Dtos.Auth
{
    public class UpdateUserDto
    {
        public required string Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public UserRole? Role { get; set; }
    }
}
