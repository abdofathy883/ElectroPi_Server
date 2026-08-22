namespace ElectroPi.Application.Dtos.Auth
{
    public class LoginDto
    {
        public required string PhoneNumber { get; set; }
        public required string Password { get; set; }
    }
}
