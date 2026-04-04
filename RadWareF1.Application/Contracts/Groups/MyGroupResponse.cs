using RadWareF1.Domain;

namespace RadWareF1.Application.Contracts.Groups;

public sealed record MyGroupResponse(
    Guid Id,
    string Name,
    GroupVisibilityEnum Visibility,
    DateTime CreatedAtUtc,
    int MembersCount);