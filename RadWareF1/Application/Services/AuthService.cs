using Microsoft.EntityFrameworkCore;
using RadWareF1.Application.Abstractions;
using RadWareF1.Application.Contracts.Auth;
using RadWareF1.Application.Contracts.Auth.LoginUser;
using RadWareF1.Application.Contracts.Auth.RegisterUser;
using RadWareF1.Domain;
using RadWareF1.Persistance;

namespace RadWareF1.Application.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(AppDbContext dbContext, IPasswordService passwordService, IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email
        };

        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id
        };

        _dbContext.Users.Add(user);
        _dbContext.Profiles.Add(profile);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterUserResponse
        {
            UserId = user.Id
        };
    }

    public async Task<LoginUserResponse> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var isValidPassword = _passwordService.VerifyPassword(user, user.PasswordHash, request.Password);

        if (!isValidPassword)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var (token, expiresAtUtc) = _jwtTokenService.GenerateToken(user);

        return new LoginUserResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}