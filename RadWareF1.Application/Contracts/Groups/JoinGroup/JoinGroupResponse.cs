namespace RadWareF1.Application.Contracts.Groups.JoinGroup;

public sealed record JoinGroupResponse(
    Guid GroupId,
    Guid UserId,
    DateTime JoinedAtUtc);