namespace EmploymentVerify.Domain.Enums;

/// <summary>
/// Indicates how POPIA consent was obtained for processing personal information.
/// </summary>
public enum ConsentType
{
    /// <summary>
    /// The requestor warrants that they have obtained consent from the individual.
    /// </summary>
    RequestorWarranted,

    /// <summary>
    /// Consent was obtained directly from the employee (e.g. via signed form).
    /// </summary>
    DirectEmployee
}
