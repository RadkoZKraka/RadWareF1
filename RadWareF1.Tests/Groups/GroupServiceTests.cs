using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RadWareF1.Application.Contracts.Groups;
using RadWareF1.Application.Services;
using RadWareF1.Domain;
using RadWareF1.Persistance;

namespace RadWareF1.Tests.Groups;

public class GroupServiceTests
{
    [Fact]
    public async Task CreateAsync_Should_Save_Owner_Membership_With_JoinedAtUtc()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        dbContext.Users.Add(new User
        {
            Id = userId,
            Email = "owner@example.com",
            PasswordHash = "hash"
        });
        await dbContext.SaveChangesAsync();

        var service = new GroupService(dbContext);
        var before = DateTime.UtcNow;

        var response = await service.CreateAsync(
            userId,
            new CreateGroupRequest
            {
                Name = "Weekend League",
                Visibility = GroupVisibilityEnum.Public
            },
            CancellationToken.None);

        var after = DateTime.UtcNow;

        var group = await dbContext.Groups.SingleAsync(x => x.Id == response.Id);
        var member = await dbContext.GroupMembers.SingleAsync(x => x.GroupId == response.Id && x.UserId == userId);

        Assert.Equal("Weekend League", response.Name);
        Assert.Equal("Weekend League", group.Name);
        Assert.Equal(GroupVisibilityEnum.Public, group.Visibility);
        Assert.Equal(GroupRoleEnum.Owner, member.Role);
        Assert.True(member.JoinedAtUtc >= before && member.JoinedAtUtc <= after);
    }
}

