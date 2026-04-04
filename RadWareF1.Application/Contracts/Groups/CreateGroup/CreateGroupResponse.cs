using RadWareF1.Domain;

namespace RadWareF1.Application.Contracts.Groups;

public class CreateGroupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string JoinCode { get; set; } = null!;
    public GroupVisibilityEnum Visibility { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}