namespace EmploymentVerify.Domain.Enums;

/// <summary>
/// The method used to verify employment.
/// </summary>
public enum VerificationMethod
{
    /// <summary>Verified via automated email to HR.</summary>
    Email = 1,

    /// <summary>Verified via phone call by an operator.</summary>
    Phone = 2
}
