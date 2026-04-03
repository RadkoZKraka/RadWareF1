using RadWareF1.Domain;

namespace RadWareF1.Application.Abstractions;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
}