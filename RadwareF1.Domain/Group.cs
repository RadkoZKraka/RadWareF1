namespace RadWareF1.Domain;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string JoinCode { get; set; } = null!;
    public GroupVisibilityEnum Visibility { get; set; }
    public List<GroupMember> Members { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
}