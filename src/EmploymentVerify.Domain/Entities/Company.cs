namespace EmploymentVerify.Domain.Entities;

/// <summary>
/// Represents a company in the verified company directory.
/// HR contacts at verified companies can receive automated email verification links.
/// </summary>
public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// South African company registration number (e.g. "2020/123456/07").
    /// </summary>
    public string RegistrationNumber { get; set; } = string.Empty;

    public string HrContactName { get; set; } = string.Empty;
    public string HrEmail { get; set; } = string.Empty;
    public string HrPhone { get; set; } = string.Empty;

    /// <summary>Physical address of the company.</summary>
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }

    /// <summary>
    /// When true, verifications for this company must be done via phone call
    /// instead of automated email.
    /// </summary>
    public bool ForceCall { get; set; }

    /// <summary>
    /// Whether the company has been verified by an admin/operator
    /// and is eligible for verifications.
    /// </summary>
    public bool IsVerified { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
