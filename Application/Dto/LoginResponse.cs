namespace Application.Dto;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = new UserDto();
    public DateTime ExpiresAt { get; set; }
}