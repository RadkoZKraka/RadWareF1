using RadWareF1.Domain;

namespace RadWareF1.Application.Contracts.Groups;

public sealed record UpdateGroupResponse(
    Guid Id,
    string Name,
    string JoinCode,
    GroupVisibilityEnum Visibility,
    DateTime CreatedAtUtc);