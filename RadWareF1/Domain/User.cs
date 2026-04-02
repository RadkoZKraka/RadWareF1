namespace RadWareF1.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public Profile Profile { get; set; } = null!;
    public List<GroupMember> GroupMembers { get; set; } = new();
}