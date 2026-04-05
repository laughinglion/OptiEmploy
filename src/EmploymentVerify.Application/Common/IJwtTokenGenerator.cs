using EmploymentVerify.Domain.Entities;

namespace EmploymentVerify.Application.Common;

/// <summary>
/// Generates JWT access tokens for authenticated users.
/// Implemented in Infrastructure; used by Application command handlers.
/// </summary>
public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
