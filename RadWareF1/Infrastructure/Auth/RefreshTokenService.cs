using System.Security.Cryptography;
using System.Text;
using RadWareF1.Application.Abstractions.Auth;
using RadWareF1.Domain;

namespace RadWareF1.Infrastructure.Auth;

public class RefreshTokenService : IRefreshTokenService
{
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    public RefreshToken Create(Guid userId, string refreshToken, DateTime utcNow, int lifetimeDays)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = ComputeHash(refreshToken),
            CreatedAtUtc = utcNow,
            ExpiresAtUtc = utcNow.AddDays(lifetimeDays)
        };
    }
}