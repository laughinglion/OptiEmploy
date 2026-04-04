namespace EmploymentVerify.Domain.Entities;

public class EmailVerificationToken
{
    public Guid Id { get; set; }
    public Guid VerificationRequestId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public VerificationRequest VerificationRequest { get; set; } = null!;
}
