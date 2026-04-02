using RadWareF1.Application.Contracts.Auth.LoginUser;
using RadWareF1.Application.Contracts.Auth.RegisterUser;

namespace RadWareF1.Application.Abstractions;

public interface IAuthService
{
    Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
    Task<LoginUserResponse> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken = default);

}