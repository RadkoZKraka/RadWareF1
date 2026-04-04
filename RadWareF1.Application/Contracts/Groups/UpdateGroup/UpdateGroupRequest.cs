using RadWareF1.Domain;

public sealed record UpdateGroupRequest(
    string? Name,
    GroupVisibilityEnum? Visibility);