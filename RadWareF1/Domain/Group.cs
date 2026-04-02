namespace RadWareF1.Domain;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string JoinCodeHash { get; set; } = null!;
    public List<GroupMember> Members { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}