using System.ComponentModel.DataAnnotations;
using ElectroPi.Domain.Enums;

namespace ElectroPi.Application.Dtos.Auth
{
    public class UpdateUserDto
    {
        [Required]
        public required string Id { get; set; }

        [StringLength(100, MinimumLength = 3)]
        public string? FullName { get; set; }

        [EmailAddress, StringLength(256)]
        public string? Email { get; set; }

        [RegularExpression(@"^\+?\d{7,15}$", ErrorMessage = "Invalid phone number.")]
        public string? PhoneNumber { get; set; }

        [EnumDataType(typeof(UserRole))]
        public UserRole? Role { get; set; }
    }
}
