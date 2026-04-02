namespace RadWareF1.Application.Abstractions;

using RadWareF1.Domain;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
}