using EmploymentVerify.Domain.Enums;

namespace EmploymentVerify.Domain.Entities;

public class OperatorNote
{
    public Guid Id { get; set; }
    public Guid VerificationRequestId { get; set; }
    public Guid OperatorId { get; set; }
    public CallOutcome CallOutcome { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public VerificationRequest VerificationRequest { get; set; } = null!;
    public User Operator { get; set; } = null!;
}
