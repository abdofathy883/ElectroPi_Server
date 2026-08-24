using System.ComponentModel.DataAnnotations;

namespace ElectroPi.Application.Dtos.Auth
{
    public class LoginDto
    {
        [Required]
        public required string PhoneNumber { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
