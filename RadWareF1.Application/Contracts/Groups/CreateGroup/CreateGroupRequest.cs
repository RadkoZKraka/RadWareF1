using RadWareF1.Domain;

namespace RadWareF1.Application.Contracts.Groups;

public class CreateGroupRequest
{
    public string Name { get; set; } = null!;
    public GroupVisibilityEnum Visibility { get; set; }
}