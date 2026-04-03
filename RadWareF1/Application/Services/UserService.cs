using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RadWareF1.Application.Contracts.Auth.User;
using RadWareF1.Persistance;
using System.Security.Claims;
using RadWareF1.Application.Abstractions;

namespace RadWareF1.Application.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<MeResponse> GetMeAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var userId = Guid.Parse(userIdClaim.Value);

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        return new MeResponse
        {
            Id = user.Id,
            Email = user.Email
        };
    }
}