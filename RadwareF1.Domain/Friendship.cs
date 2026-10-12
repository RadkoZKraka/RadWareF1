namespace RadWareF1.Domain;

public class Friendship
{
    public Guid Id { get; set; }
    public Guid UserAId { get; set; }
    public User UserA { get; set; } = null!;
    public Guid UserBId { get; set; }
    public User UserB { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
