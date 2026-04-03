using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RadWareF1.Application.Abstractions;
using RadWareF1.Application.Abstractions.Auth;
using RadWareF1.Application.Contracts.Auth;
using RadWareF1.Application.Contracts.Auth.Login;
using RadWareF1.Application.Contracts.Auth.LoginUser;
using RadWareF1.Application.Contracts.Auth.Logout;
using RadWareF1.Application.Contracts.Auth.RefreshToken;
using RadWareF1.Application.Contracts.Auth.RegisterUser;
using RadWareF1.Domain;
using RadWareF1.Persistance;

namespace RadWareF1.Application.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        AppDbContext dbContext,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var existingUser = await _dbContext.Users
            .AnyAsync(x => x.Email == request.Email, cancellationToken);

        if (existingUser)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email
        };

        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterUserResponse
        {
            UserId = user.Id
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        User? user = await _dbContext.Users
            .SingleOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

        if (user is null || !_passwordService.VerifyPassword(user, user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var (accessToken, accessTokenExpiresAtUtc) = _jwtTokenService.GenerateToken(user);
        var refreshToken = _refreshTokenService.GenerateRefreshToken();
        var refreshTokenEntity = _refreshTokenService.Create(
            user.Id,
            refreshToken,
            DateTime.UtcNow,
            _jwtOptions.RefreshTokenDays);

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc
        };
    }

    public async Task<AuthTokensResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenService.ComputeHash(request.RefreshToken);

        var existingRefreshToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (existingRefreshToken is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (existingRefreshToken.RevokedAtUtc != null || existingRefreshToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token is no longer active.");
        }

        var (newAccessToken, accessTokenExpiresAtUtc) = _jwtTokenService.GenerateToken(existingRefreshToken.User);
        var newRefreshToken = _refreshTokenService.GenerateRefreshToken();
        var newRefreshTokenEntity = _refreshTokenService.Create(
            existingRefreshToken.User.Id,
            newRefreshToken,
            DateTime.UtcNow,
            _jwtOptions.RefreshTokenDays);

        existingRefreshToken.RevokedAtUtc = DateTime.UtcNow;
        existingRefreshToken.ReplacedByTokenHash = newRefreshTokenEntity.TokenHash;

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokensResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc
        };
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenService.ComputeHash(request.RefreshToken);

        var existingRefreshToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (existingRefreshToken is null)
        {
            return;
        }

        if (existingRefreshToken.RevokedAtUtc == null)
        {
            existingRefreshToken.RevokedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}