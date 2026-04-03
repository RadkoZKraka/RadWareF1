namespace RadWareF1.Application.Contracts.Auth.Login;

public class LoginResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime AccessTokenExpiresAtUtc { get; set; }
}