namespace RadWareF1.Application.Contracts.Groups;

public sealed record PublicGroupResponse(
    Guid Id,
    string Name,
    int MembersCount);