namespace RadWareF1.Application.Contracts.Auth;

public class JwtOptions
{
    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpiryMinutes { get; set; }
    public int RefreshTokenDays { get; set; }
}