using RadWareF1.Application.Contracts.Auth;
using RadWareF1.Application.Contracts.Auth.Login;
using RadWareF1.Application.Contracts.Auth.LoginUser;
using RadWareF1.Application.Contracts.Auth.Logout;
using RadWareF1.Application.Contracts.Auth.RefreshToken;
using RadWareF1.Application.Contracts.Auth.RegisterUser;

namespace RadWareF1.Application.Abstractions;

public interface IAuthService
{
    Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthTokensResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);
}