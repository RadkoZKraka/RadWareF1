using RadWareF1.Application.Contracts.Groups;
using RadWareF1.Application.Contracts.Groups.JoinGroup;

namespace RadWareF1.Application.Abstractions;

public interface IGroupService
{
    Task<CreateGroupResponse> CreateAsync(Guid userId, CreateGroupRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicGroupResponse>> GetPublicGroupsAsync(CancellationToken cancellationToken);
    Task<GroupDetailsResponse> GetByIdAsync(Guid groupId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MyGroupResponse>> GetMineAsync(Guid userId, CancellationToken cancellationToken);
    Task<JoinGroupResponse> JoinAsync(JoinGroupRequest request, Guid userId, CancellationToken cancellationToken);
    Task LeaveAsync(Guid groupId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GroupMembersResponse>> GetMembersAsync(Guid groupId, Guid userId, CancellationToken cancellationToken);
    Task<UpdateGroupResponse> UpdateAsync(Guid groupId, Guid userId, UpdateGroupRequest request, CancellationToken cancellationToken);
}
