namespace EmploymentVerify.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public bool IsSent => SentAt.HasValue;
}
