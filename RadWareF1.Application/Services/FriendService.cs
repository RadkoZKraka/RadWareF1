using Microsoft.EntityFrameworkCore;
using RadWareF1.Application.Abstractions;
using RadWareF1.Application.Contracts.Friends;
using RadWareF1.Domain;
using RadWareF1.Persistance;

namespace RadWareF1.Application.Services;

public class FriendService : IFriendService
{
    private readonly AppDbContext _dbContext;

    public FriendService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<FriendResponse>> GetFriendsAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var friendships = await _dbContext.Friendships
            .AsNoTracking()
            .Where(x => x.UserAId == currentUserId || x.UserBId == currentUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.UserAId,
                x.UserBId,
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        if (friendships.Count == 0)
        {
            return Array.Empty<FriendResponse>();
        }

        var friendIds = friendships
            .Select(x => x.UserAId == currentUserId ? x.UserBId : x.UserAId)
            .ToArray();

        var usersById = await _dbContext.Users
            .AsNoTracking()
            .Where(x => friendIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return friendships
            .Select(x =>
            {
                var friendId = x.UserAId == currentUserId ? x.UserBId : x.UserAId;
                var friend = usersById[friendId];

                return new FriendResponse
                {
                    UserId = friend.Id,
                    Email = friend.Email,
                    AddedAtUtc = x.CreatedAtUtc
                };
            })
            .ToList();
    }

    public async Task<AddFriendResponse> AddFriendAsync(Guid currentUserId, AddFriendRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier))
        {
            throw new InvalidOperationException("Please provide a user id or email.");
        }

        var identifier = request.Identifier.Trim();

        var targetUser = await ResolveUserAsync(identifier, cancellationToken);
        if (targetUser is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (targetUser.Id == currentUserId)
        {
            throw new InvalidOperationException("You cannot add yourself as a friend.");
        }

        var (userAId, userBId) = OrderPair(currentUserId, targetUser.Id);
        var existing = await _dbContext.Friendships
            .SingleOrDefaultAsync(x => x.UserAId == userAId && x.UserBId == userBId, cancellationToken);
        var created = false;

        if (existing is null)
        {
            existing = new Friendship
            {
                Id = Guid.NewGuid(),
                UserAId = userAId,
                UserBId = userBId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.Friendships.Add(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
            created = true;
        }

        return new AddFriendResponse
        {
            Friend = new FriendResponse
            {
                UserId = targetUser.Id,
                Email = targetUser.Email,
                AddedAtUtc = existing.CreatedAtUtc
            },
            IsNewFriend = created
        };
    }

    private async Task<User?> ResolveUserAsync(string identifier, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(identifier, out var userId))
        {
            return await _dbContext.Users
                .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        }

        var normalizedEmail = identifier.ToLower();
        return await _dbContext.Users
            .SingleOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    private static (Guid UserAId, Guid UserBId) OrderPair(Guid first, Guid second)
    {
        return first.CompareTo(second) <= 0
            ? (first, second)
            : (second, first);
    }
}



