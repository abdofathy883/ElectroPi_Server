using System.ComponentModel.DataAnnotations;

namespace ElectroPi.Application.Dtos.Password
{
    public class ChangePasswordDto
    {
        [Required]
        public required string Id { get; set; }

        [Required]
        public required string OldPassword { get; set; }

        [Required, MinLength(6)]
        public required string NewPassword { get; set; }

        [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public required string ConfirmNewPassword { get; set; }
    }
}
