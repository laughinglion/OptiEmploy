using EmploymentVerify.Domain.Enums;

namespace EmploymentVerify.Domain.Entities;

public class VerificationResponse
{
    public Guid Id { get; set; }
    public Guid VerificationRequestId { get; set; }
    public string RespondedBy { get; set; } = string.Empty;
    public ResponseType ResponseType { get; set; }
    public string? ConfirmedJobTitle { get; set; }
    public DateOnly? ConfirmedStartDate { get; set; }
    public DateOnly? ConfirmedEndDate { get; set; }
    public bool? IsCurrentlyEmployed { get; set; }
    public string? Notes { get; set; }
    public DateTime RespondedAt { get; set; }
    public VerificationRequest VerificationRequest { get; set; } = null!;
}
