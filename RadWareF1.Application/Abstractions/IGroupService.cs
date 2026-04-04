using RadWareF1.Application.Contracts.Groups;

namespace RadWareF1.Application.Abstractions;

public interface IGroupService
{
    Task<CreateGroupResponse> CreateAsync(Guid userId, CreateGroupRequest request, CancellationToken cancellationToken);
}