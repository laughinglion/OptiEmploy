namespace EmploymentVerify.Domain.Entities;

public class AuditEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public Guid? TargetUserId { get; set; }
    public string? Metadata { get; set; }
    public DateTime OccurredAt { get; set; }
}
