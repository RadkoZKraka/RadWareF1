using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RadWareF1.Application.Contracts.Auth.Login;
using RadWareF1.Application.Contracts.Auth.LoginUser;
using RadWareF1.Application.Contracts.Auth.RegisterUser;
using RadWareF1.Application.Contracts.Friends;
using RadWareF1.Domain;
using RadWareF1.Persistance;
using RadWareF1.Tests.Infrastructure;

namespace RadWareF1.Tests.Friends;

public class FriendsEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public FriendsEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddFriend_Should_Add_Friend_By_Email_And_List_It()
    {
        var client = _factory.CreateClient();
        var loginResponse = await LoginAsync(client, "test@test.com", "test123");
        client.SetBearerToken(loginResponse.AccessToken);

        var friendEmail = $"friend-{Guid.NewGuid():N}@test.com";
        await RegisterFriendAsync(client, friendEmail);

        var addResponse = await client.PostAsJsonAsync("/api/friends", new AddFriendRequest
        {
            Identifier = friendEmail
        });

        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var addResult = await addResponse.ReadAsJsonAsync<AddFriendResponse>();
        Assert.NotNull(addResult);
        Assert.True(addResult!.IsNewFriend);
        Assert.Equal(friendEmail, addResult.Friend.Email);

        var listResponse = await client.GetAsync("/api/friends");
        var friends = await listResponse.ReadAsJsonAsync<List<FriendResponse>>();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(friends);
        Assert.Contains(friends!, x => x.Email == friendEmail);
    }

    [Fact]
    public async Task AddFriend_Should_Add_Friend_By_UserId()
    {
        var client = _factory.CreateClient();
        var loginResponse = await LoginAsync(client, "test@test.com", "test123");
        client.SetBearerToken(loginResponse.AccessToken);

        var friendEmail = $"friend-{Guid.NewGuid():N}@test.com";
        var registerResult = await RegisterFriendAsync(client, friendEmail);

        var addResponse = await client.PostAsJsonAsync("/api/friends", new AddFriendRequest
        {
            Identifier = registerResult!.UserId.ToString()
        });

        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var addResult = await addResponse.ReadAsJsonAsync<AddFriendResponse>();
        Assert.NotNull(addResult);
        Assert.True(addResult!.IsNewFriend);
        Assert.Equal(friendEmail, addResult.Friend.Email);
    }

    [Fact]
    public async Task AddFriend_Should_Return_BadRequest_When_Adding_Self()
    {
        var client = _factory.CreateClient();
        var loginResponse = await LoginAsync(client, "test@test.com", "test123");
        client.SetBearerToken(loginResponse.AccessToken);

        var selfResponse = await client.PostAsJsonAsync("/api/friends", new AddFriendRequest
        {
            Identifier = "test@test.com"
        });

        Assert.Equal(HttpStatusCode.BadRequest, selfResponse.StatusCode);
    }

    private static async Task<LoginResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        var result = await response.ReadAsJsonAsync<LoginResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        return result!;
    }

    private static async Task<RegisterUserResponse> RegisterFriendAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest
        {
            Email = email,
            UserName = "Friend",
            Password = "test123"
        });

        var result = await response.ReadAsJsonAsync<RegisterUserResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        return result!;
    }
}

