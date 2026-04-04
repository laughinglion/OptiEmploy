namespace EmploymentVerify.Domain.Enums;

/// <summary>
/// Represents the current status of a verification request.
/// </summary>
public enum VerificationStatus
{
    Pending,
    InProgress,
    Confirmed,
    Denied,
    Failed
}
