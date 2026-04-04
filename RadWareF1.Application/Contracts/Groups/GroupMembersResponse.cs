using RadWareF1.Domain;

namespace RadWareF1.Application.Contracts.Groups;

public sealed record GroupMembersResponse(
    Guid UserId,
    GroupRoleEnum Role);