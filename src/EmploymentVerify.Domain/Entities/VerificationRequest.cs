using EmploymentVerify.Domain.Enums;

namespace EmploymentVerify.Domain.Entities;

/// <summary>
/// Represents an employment verification request submitted by a requestor.
/// Contains employee personal information (subject to POPIA) and consent records.
/// </summary>
public class VerificationRequest
{
    public Guid Id { get; set; }

    /// <summary>The user (requestor) who submitted this verification.</summary>
    public Guid RequestorId { get; set; }

    // ── Employee Details ──

    public string EmployeeFullName { get; set; } = string.Empty;
    public IdentificationType IdType { get; set; }

    /// <summary>
    /// SA ID number — encrypted at rest for POPIA compliance.
    /// Only populated when IdType is SaIdNumber.
    /// </summary>
    public string? SaIdNumber { get; set; }

    /// <summary>Only populated when IdType is Passport.</summary>
    public string? PassportNumber { get; set; }

    /// <summary>Only populated when IdType is Passport.</summary>
    public string? PassportCountry { get; set; }

    // ── Employment Details ──

    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// If the company was selected from the verified directory, its ID is stored here.
    /// Null means the requestor typed a company name manually.
    /// </summary>
    public Guid? CompanyId { get; set; }

    public string JobTitle { get; set; } = string.Empty;
    public DateOnly EmploymentStartDate { get; set; }
    public DateOnly? EmploymentEndDate { get; set; }

    // ── HR Contact (optional override or from company directory) ──

    public string? HrContactName { get; set; }
    public string? HrEmail { get; set; }
    public string? HrPhone { get; set; }

    // ── POPIA Consent (auditable) ──

    /// <summary>
    /// Whether the requestor affirmed POPIA consent for processing the individual's
    /// personal information. Must be true for submission.
    /// </summary>
    public bool PopiaConsentGiven { get; set; }

    /// <summary>
    /// Whether the requestor affirmed accuracy of the information provided.
    /// Must be true for submission.
    /// </summary>
    public bool AccuracyConfirmed { get; set; }

    /// <summary>
    /// How consent was obtained — requestor-warranted or direct from employee.
    /// </summary>
    public ConsentType ConsentType { get; set; }

    /// <summary>
    /// Timestamp when POPIA consent was recorded. Used for audit trail.
    /// </summary>
    public DateTime ConsentRecordedAt { get; set; }

    // ── Status & Workflow ──

    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

    /// <summary>How the verification was completed — set when Status moves to Confirmed or Denied.</summary>
    public VerificationMethod? VerificationMethod { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // ── Navigation ──

    public User? Requestor { get; set; }
    public Company? Company { get; set; }
}
