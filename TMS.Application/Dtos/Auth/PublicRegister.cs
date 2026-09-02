using System.ComponentModel.DataAnnotations;

namespace ElectroPi.Application.Dtos.Auth
{
    public class PublicRegister
    {
        [Required, StringLength(100, MinimumLength = 3)]
        public required string FullName { get; set; }

        [Required, EmailAddress, StringLength(256)]
        public required string Email { get; set; }

        [Required, RegularExpression(@"^\+?\d{7,15}$", ErrorMessage = "Invalid phone number.")]
        public required string PhoneNumber { get; set; }

        [Required, MinLength(6)]
        public required string Password { get; set; }
    }
}
