using System.ComponentModel.DataAnnotations;
using ElectroPi.Domain.Enums;

namespace ElectroPi.Application.Dtos.Auth
{
    public class RegisterDto
    {
        [Required, StringLength(100, MinimumLength = 3)]
        public required string FullName { get; set; }

        [Required, EmailAddress, StringLength(256)]
        public required string Email { get; set; }

        [Required, RegularExpression(@"^\+?\d{7,15}$", ErrorMessage = "Invalid phone number.")]
        public required string PhoneNumber { get; set; }

        [Required, MinLength(6)]
        public required string Password { get; set; }

        [Required, EnumDataType(typeof(UserRole))]
        public required UserRole Role { get; set; }
    }
}
