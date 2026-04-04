namespace RadWareF1.Application.Contracts.Auth.Common;

public class AuthTokensResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime AccessTokenExpiresAtUtc { get; set; }
}