namespace RadWareF1.Application.Contracts.Auth.User;

public class MeResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
}