using RadWareF1.Domain;

namespace RadWareF1.Application.Abstractions;

public interface IPasswordService
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string hashedPassword, string providedPassword);
}