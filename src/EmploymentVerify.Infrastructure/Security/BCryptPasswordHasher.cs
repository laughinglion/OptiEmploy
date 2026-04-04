using EmploymentVerify.Application.Common;

namespace EmploymentVerify.Infrastructure.Security;

/// <summary>
/// Password hasher using BCrypt for secure password storage.
/// BCrypt is designed for password hashing with built-in salting and configurable work factor.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
