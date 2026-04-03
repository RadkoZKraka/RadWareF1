using RadWareF1.Domain;

namespace RadWareF1.Application.Abstractions.Auth;

public interface IRefreshTokenService
{
    string GenerateRefreshToken();
    string ComputeHash(string value);
    RefreshToken Create(Guid userId, string refreshToken, DateTime utcNow, int lifetimeDays);
}