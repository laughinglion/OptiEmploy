using System.Security.Cryptography;
using EmploymentVerify.Application.Common;

namespace EmploymentVerify.Infrastructure.Security;

public class CryptoTokenGenerator : IEmailVerificationTokenGenerator
{
    public string GenerateToken()
    {
        // Generate a cryptographically secure URL-safe token
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
