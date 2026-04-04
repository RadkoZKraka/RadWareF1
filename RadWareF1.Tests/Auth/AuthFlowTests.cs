using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RadWareF1.Application.Contracts.Auth.Common;
using RadWareF1.Application.Contracts.Auth.Login;
using RadWareF1.Application.Contracts.Auth.LoginUser;
using RadWareF1.Application.Contracts.Auth.RefreshToken;
using RadWareF1.Application.Contracts.Auth.User;
using RadWareF1.Domain;
using RadWareF1.Persistance;
using RadWareF1.Tests.Infrastructure;

namespace RadWareF1.Tests.Auth;

public class AuthFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_Should_Return_Ok_And_Tokens_When_Credentials_Are_Valid()
    {
        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "test123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        var result = await response.ReadAsJsonAsync<LoginResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_Password_Is_Invalid()
    {
        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "wrong-password"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_User_Does_Not_Exist()
    {
        var request = new LoginRequest
        {
            Email = "missing@test.com",
            Password = "test123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        var response = await _client.GetAsync("/api/user/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_Should_Return_Current_User_When_Token_Is_Valid()
    {
        var loginResponse = await LoginAsync();

        _client.SetBearerToken(loginResponse.AccessToken);

        var response = await _client.GetAsync("/api/user/me");
        var result = await response.ReadAsJsonAsync<MeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("test@test.com", result!.Email);
    }

    [Fact]
    public async Task Refresh_Should_Return_New_Tokens_When_RefreshToken_Is_Valid()
    {
        var loginResponse = await LoginAsync();

        var request = new RefreshTokenRequest
        {
            RefreshToken = loginResponse.RefreshToken
        };

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);
        var result = await response.ReadAsJsonAsync<AuthTokensResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.NotEqual(loginResponse.AccessToken, result.AccessToken);
        Assert.NotEqual(loginResponse.RefreshToken, result.RefreshToken);
    }

    [Fact]
    public async Task Refresh_Should_Return_Unauthorized_When_RefreshToken_Is_Invalid()
    {
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_Should_Invalidate_Previous_RefreshToken_When_Rotated()
    {
        var loginResponse = await LoginAsync();

        var firstRefreshRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResponse.RefreshToken
        };

        var firstRefreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", firstRefreshRequest);
        var firstRefreshResult = await firstRefreshResponse.ReadAsJsonAsync<AuthTokensResponse>();

        Assert.Equal(HttpStatusCode.OK, firstRefreshResponse.StatusCode);
        Assert.NotNull(firstRefreshResult);

        var secondRefreshWithOldTokenRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResponse.RefreshToken
        };

        var secondRefreshWithOldTokenResponse = await _client.PostAsJsonAsync("/api/auth/refresh", secondRefreshWithOldTokenRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, secondRefreshWithOldTokenResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_Should_Save_New_RefreshToken_In_Database()
    {
        var loginResponse = await LoginAsync();

        var request = new RefreshTokenRequest
        {
            RefreshToken = loginResponse.RefreshToken
        };

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);
        var result = await response.ReadAsJsonAsync<AuthTokensResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = dbContext.Users.Single(x => x.Email == "test@test.com");

        var savedToken = dbContext.RefreshTokens
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.Id)
            .FirstOrDefault();

        Assert.NotNull(savedToken);
    }
    private async Task<LoginResponse> LoginAsync()
    {
        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "test123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        var result = await response.ReadAsJsonAsync<LoginResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);

        return result!;
    }
}