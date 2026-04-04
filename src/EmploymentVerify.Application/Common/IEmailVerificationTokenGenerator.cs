namespace EmploymentVerify.Application.Common;

public interface IEmailVerificationTokenGenerator
{
    string GenerateToken();
}
