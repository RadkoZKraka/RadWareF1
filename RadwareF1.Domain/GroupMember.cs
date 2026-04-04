namespace RadWareF1.Domain;

public class GroupMember
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public GroupRoleEnum Role { get; set; }

    public DateTime JoinedAtUtc { get; set; }
}