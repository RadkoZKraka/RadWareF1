namespace RadWareF1.Application.Contracts.Auth.Logout;

public class LogoutRequest
{
    public string RefreshToken { get; set; } = null!;
}