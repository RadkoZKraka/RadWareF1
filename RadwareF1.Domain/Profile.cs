namespace RadWareF1.Domain;

public class Profile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Username { get; set; } = string.Empty;
}