using Microsoft.EntityFrameworkCore;
using RadWareF1.Application.Abstractions;
using RadWareF1.Application.Common;
using RadWareF1.Application.Contracts.Groups;
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
            GroupId = group.Id,
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