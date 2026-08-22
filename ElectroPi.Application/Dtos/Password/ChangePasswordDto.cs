namespace ElectroPi.Application.Dtos.Password
{
    public class ChangePasswordDto
    {
        public required string Id { get; set; }
        public required string OldPassword { get; set; }
        public required string NewPassword { get; set; }
        public required string ConfirmNewPassword { get; set; }
    }
}
