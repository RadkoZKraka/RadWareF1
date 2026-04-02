namespace RadWareF1.Domain;

public class Profile
{
    public Guid UserId { get; set; }
    public string Nickname { get; set; } = default!;
}