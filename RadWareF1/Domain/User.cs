namespace RadWareF1.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}