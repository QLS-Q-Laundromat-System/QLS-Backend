namespace QLS.Backend.DTOs
{
    public class LoginResponse
    {
        public TokenDto Tokens { get; set; } = new();
        public UserDto User { get; set; } = new();
    }
}
