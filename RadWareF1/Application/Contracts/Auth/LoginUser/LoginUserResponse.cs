namespace RadWareF1.Application.Contracts.Auth.LoginUser;

public class LoginUserResponse
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
}