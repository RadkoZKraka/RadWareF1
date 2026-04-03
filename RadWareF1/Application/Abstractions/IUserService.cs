using RadWareF1.Application.Contracts.Auth.User;

namespace RadWareF1.Application.Abstractions;

public interface IUserService
{
    Task<MeResponse> GetMeAsync(CancellationToken cancellationToken);
}