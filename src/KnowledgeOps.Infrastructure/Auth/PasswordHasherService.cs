using KnowledgeOps.Application.Auth.Abstractions;

namespace KnowledgeOps.Infrastructure.Auth;

internal sealed class PasswordHasherService : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string hashedPassword, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
