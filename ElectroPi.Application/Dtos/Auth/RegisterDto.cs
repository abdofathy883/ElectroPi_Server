using ElectroPi.Domain.Enums;

namespace ElectroPi.Application.Dtos.Auth
{
    public class RegisterDto
    {
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Password { get; set; }
        public required UserRole Role { get; set; }
    }
}
