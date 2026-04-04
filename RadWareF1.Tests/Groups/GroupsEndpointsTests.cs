using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RadWareF1.Application.Contracts.Groups;
using RadWareF1.Application.Contracts.Groups.JoinGroup;
using RadWareF1.Domain;
using RadWareF1.Persistance;
using RadWareF1.Tests.Infrastructure;

namespace RadWareF1.Tests.Groups;

public class GroupsEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public GroupsEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Should_Create_Group_And_Add_Owner()
    {
        var client = _factory.CreateClient();
        var userId = Guid.NewGuid();

        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var request = new CreateGroupRequest
        {
            Name = "Test Group",
            Visibility = GroupVisibilityEnum.Public
        };

        var response = await client.PostAsJsonAsync("/api/groups", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var group = db.Groups.Single();
        var member = db.GroupMembers.Single();

        Assert.Equal("Test Group", group.Name);
        Assert.Equal(GroupVisibilityEnum.Public, group.Visibility);
        Assert.Equal(group.Id, member.GroupId);
        Assert.Equal(userId, member.UserId);
        Assert.Equal(GroupRoleEnum.Owner, member.Role);
        Assert.False(string.IsNullOrWhiteSpace(group.JoinCode));
    }

    [Fact]
    public async Task Join_Should_Add_User_To_Group()
    {
        var ownerId = Guid.NewGuid();
        var joiningUserId = Guid.NewGuid();
        Guid groupId;
        const string joinCode = "ABC123";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = "My Group",
                Visibility = GroupVisibilityEnum.Public,
                JoinCode = joinCode,
                CreatedAtUtc = DateTime.UtcNow
            };

            groupId = group.Id;

            db.Groups.Add(group);
            db.GroupMembers.Add(new GroupMember
            {
                GroupId = group.Id,
                UserId = ownerId,
                Role = GroupRoleEnum.Owner
            });

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", joiningUserId.ToString());

        var request = new JoinGroupRequest
        {
            JoinCode = joinCode
        };

        var response = await client.PostAsJsonAsync("/api/groups/join", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var member = verifyDb.GroupMembers.SingleOrDefault(x => x.GroupId == groupId && x.UserId == joiningUserId);

        Assert.NotNull(member);
        Assert.Equal(GroupRoleEnum.User, member.Role);
    }

    [Fact]
    public async Task Join_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());

        var request = new JoinGroupRequest
        {
            JoinCode = "WRONG123"
        };

        var response = await client.PostAsJsonAsync("/groups/join", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Leave_Should_Remove_Member_From_Group()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Groups.Add(new Group
            {
                Id = groupId,
                Name = "Group",
                Visibility = GroupVisibilityEnum.Public,
                JoinCode = "LEAVE1",
                CreatedAtUtc = DateTime.UtcNow
            });

            db.GroupMembers.Add(new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                Role = GroupRoleEnum.User
            });

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var response = await client.PostAsync($"/api/groups/{groupId}/leave", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var member = verifyDb.GroupMembers.SingleOrDefault(x => x.GroupId == groupId && x.UserId == userId);

        Assert.Null(member);
    }

    [Fact]
    public async Task GetById_Should_Return_Group()
    {
        var ownerId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Groups.Add(new Group
            {
                Id = groupId,
                Name = "Read Group",
                Visibility = GroupVisibilityEnum.Public,
                JoinCode = "READ01",
                CreatedAtUtc = DateTime.UtcNow
            });

            db.GroupMembers.Add(new GroupMember
            {
                GroupId = groupId,
                UserId = ownerId,
                Role = GroupRoleEnum.Owner
            });

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", ownerId.ToString());

        var response = await client.GetAsync($"/api/groups/{groupId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMembers_Should_Return_Group_Members()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Groups.Add(new Group
            {
                Id = groupId,
                Name = "Members Group",
                Visibility = GroupVisibilityEnum.Public,
                JoinCode = "MEM001",
                CreatedAtUtc = DateTime.UtcNow
            });

            db.GroupMembers.AddRange(
                new GroupMember
                {
                    GroupId = groupId,
                    UserId = ownerId,
                    Role = GroupRoleEnum.Owner
                },
                new GroupMember
                {
                    GroupId = groupId,
                    UserId = memberId,
                    Role = GroupRoleEnum.User
                });

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", ownerId.ToString());

        var response = await client.GetAsync($"/api/groups/{groupId}/members");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(ownerId.ToString(), body);
        Assert.Contains(memberId.ToString(), body);
    }
}