using Microsoft.EntityFrameworkCore;
using RadWareF1.Application.Abstractions;
using RadWareF1.Application.Common;
using RadWareF1.Application.Contracts.Groups;
using RadWareF1.Application.Contracts.Groups.JoinGroup;
using RadWareF1.Domain;
using RadWareF1.Persistance;

namespace RadWareF1.Application.Services;

public class GroupService : IGroupService
{
    private readonly AppDbContext _dbContext;

    public GroupService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateGroupResponse> CreateAsync(
        Guid userId,
        CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var joinCode = await GenerateUniqueJoinCodeAsync(cancellationToken);

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Visibility = request.Visibility,
            JoinCode = joinCode,
            CreatedAtUtc = DateTime.UtcNow
        };

        group.Members.Add(new GroupMember
        {
            UserId = userId,
            Role = GroupRoleEnum.Owner
        });

        _dbContext.Groups.Add(group);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateGroupResponse
        {
            Id = group.Id,
            Name = group.Name,
            JoinCode = group.JoinCode,
            Visibility = group.Visibility,
            CreatedAtUtc = group.CreatedAtUtc
        };
    }

    public async Task<IReadOnlyList<PublicGroupResponse>> GetPublicGroupsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Groups
            .AsNoTracking()
            .Where(x => x.Visibility == GroupVisibilityEnum.Public)
            .OrderBy(x => x.Name)
            .Select(x => new PublicGroupResponse(
                x.Id,
                x.Name,
                x.Members.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupDetailsResponse> GetByIdAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var group = await _dbContext.Groups
            .AsNoTracking()
            .Where(x => x.Id == groupId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Visibility,
                x.CreatedAtUtc,
                MembersCount = x.Members.Count,
                IsMember = x.Members.Any(m => m.UserId == userId)
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (group is null)
        {
            throw new KeyNotFoundException("Group not found.");
        }

        return new GroupDetailsResponse(
            group.Id,
            group.Name,
            group.Visibility,
            group.CreatedAtUtc,
            group.MembersCount,
            group.IsMember);
    }

    public async Task<IReadOnlyList<MyGroupResponse>> GetMineAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Groups
            .AsNoTracking()
            .Where(x => x.Members.Any(m => m.UserId == userId))
            .OrderBy(x => x.Name)
            .Select(x => new MyGroupResponse(
                x.Id,
                x.Name,
                x.Visibility,
                x.CreatedAtUtc,
                x.Members.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<JoinGroupResponse> JoinAsync(
        JoinGroupRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var group = await _dbContext.Groups
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.JoinCode == request.JoinCode, cancellationToken);

        if (group is null)
        {
            throw new KeyNotFoundException("Group not found.");
        }

        var isAlreadyMember = await _dbContext.GroupMembers
            .AnyAsync(x => x.GroupId == group.Id && x.UserId == userId, cancellationToken);

        if (isAlreadyMember)
        {
            throw new InvalidOperationException("User is already a member of this group.");
        }

        if (group.Visibility != GroupVisibilityEnum.Public)
        {
            throw new InvalidOperationException("This group cannot be joined directly.");
        }

        var joinedAtUtc = DateTime.UtcNow;

        var groupMember = new GroupMember
        {
            GroupId = group.Id,
            UserId = userId,
            Role = GroupRoleEnum.User,
            JoinedAtUtc = joinedAtUtc
        };

        _dbContext.GroupMembers.Add(groupMember);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new JoinGroupResponse(group.Id, userId, joinedAtUtc);
    }

    public async Task LeaveAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var membership = await _dbContext.GroupMembers
            .SingleOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId, cancellationToken);

        if (membership is null)
        {
            throw new KeyNotFoundException("Group membership not found.");
        }

        if (membership.Role == GroupRoleEnum.Owner)
        {
            throw new InvalidOperationException("Group owner cannot leave the group.");
        }

        _dbContext.GroupMembers.Remove(membership);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GroupMembersResponse>> GetMembersAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var groupExists = await _dbContext.Groups
            .AsNoTracking()
            .AnyAsync(x => x.Id == groupId, cancellationToken);

        if (!groupExists)
        {
            throw new KeyNotFoundException("Group not found.");
        }

        var isMember = await _dbContext.GroupMembers
            .AnyAsync(x => x.GroupId == groupId && x.UserId == userId, cancellationToken);

        if (!isMember)
        {
            throw new UnauthorizedAccessException("User is not a member of this group.");
        }

        return await _dbContext.GroupMembers
            .AsNoTracking()
            .Where(x => x.GroupId == groupId)
            .OrderByDescending(x => x.Role)
            .ThenBy(x => x.UserId)
            .Select(x => new GroupMembersResponse(
                x.UserId,
                x.Role))
            .ToListAsync(cancellationToken);
    }

    public async Task<UpdateGroupResponse> UpdateAsync(
        Guid groupId,
        Guid userId,
        UpdateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var group = await _dbContext.Groups
            .SingleOrDefaultAsync(x => x.Id == groupId, cancellationToken);

        if (group is null)
        {
            throw new KeyNotFoundException("Group not found.");
        }

        var membership = await _dbContext.GroupMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId, cancellationToken);

        if (membership is null)
        {
            throw new UnauthorizedAccessException("User is not a member of this group.");
        }

        if (membership.Role != GroupRoleEnum.Owner)
        {
            throw new UnauthorizedAccessException("Only group owner can update the group.");
        }

        group.Name = request.Name ?? group.Name;
        group.Visibility = request.Visibility ?? group.Visibility;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateGroupResponse(
            group.Id,
            group.Name,
            group.JoinCode,
            group.Visibility,
            group.CreatedAtUtc);
    }

    private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var code = GenerateJoinCode();

            var exists = await _dbContext.Groups
                .AnyAsync(x => x.JoinCode == code, cancellationToken);

            if (!exists)
            {
                return code;
            }
        }
    }

    private static string GenerateJoinCode()
    {
        var chars = new char[Constants.JOIN_CODE_LENGTH];

        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Constants.JOIN_CODE_CHARS[Random.Shared.Next(Constants.JOIN_CODE_CHARS.Length)];
        }

        return new string(chars);
    }
}