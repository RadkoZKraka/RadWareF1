using RadWareF1.Domain;

namespace RadWareF1.Application.Contracts.Groups;

public sealed record GroupDetailsResponse(
    Guid Id,
    string Name,
    GroupVisibilityEnum Visibility,
    DateTime CreatedAtUtc,
    int MembersCount,
    bool IsMember);