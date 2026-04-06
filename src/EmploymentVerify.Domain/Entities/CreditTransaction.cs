namespace EmploymentVerify.Domain.Entities;

public class CreditTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }       // positive = credit, negative = debit
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string TransactionType { get; set; } = string.Empty; // "Debit" | "Credit"
    public string Reason { get; set; } = string.Empty;
    public Guid? RelatedVerificationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
}
