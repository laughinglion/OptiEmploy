namespace EmploymentVerify.Domain.Constants;

/// <summary>
/// Pricing constants for the pay-per-verification model.
/// </summary>
public static class Pricing
{
    /// <summary>
    /// Cost in credits deducted per verification request submitted.
    /// 1 credit = R1.00 (South African Rand).
    /// </summary>
    public const decimal VerificationCost = 1.00m;
}
