namespace RadWareF1.Application.Contracts.Auth.RegisterUser;

public class RegisterUserRequest
{
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
}