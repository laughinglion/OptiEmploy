using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Admin.Queries;

public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, PagedResult<AuditLogEntryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AuditLogEntryDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        // Compose a unified event timeline from multiple tables
        var verificationEvents = await _context.VerificationRequests
            .AsNoTracking()
            .Select(v => new AuditLogEntryDto(
                v.CreatedAt,
                "VerificationSubmitted",
                $"Verification for {v.EmployeeFullName} at {v.CompanyName} submitted ({v.Status})",
                null,
                v.Id.ToString()))
            .ToListAsync(cancellationToken);

        var creditEvents = await _context.CreditTransactions
            .AsNoTracking()
            .Include(t => t.User)
            .Select(t => new AuditLogEntryDto(
                t.CreatedAt,
                "CreditTransaction",
                $"{t.TransactionType}: {t.Amount:F2} credits — {t.Reason}",
                t.User != null ? t.User.Email : null,
                t.RelatedVerificationId.HasValue ? t.RelatedVerificationId.Value.ToString() : null))
            .ToListAsync(cancellationToken);

        var operatorNoteEvents = await _context.OperatorNotes
            .AsNoTracking()
            .Include(n => n.Operator)
            .Select(n => new AuditLogEntryDto(
                n.CreatedAt,
                "OperatorCall",
                $"Operator call recorded: {n.CallOutcome}",
                n.Operator.Email,
                n.VerificationRequestId.ToString()))
            .ToListAsync(cancellationToken);

        var auditEvents = await _context.AuditEvents
            .AsNoTracking()
            .Select(a => new AuditLogEntryDto(
                a.OccurredAt,
                a.EventType,
                a.Description,
                null,
                a.TargetUserId.HasValue ? a.TargetUserId.Value.ToString() : null))
            .ToListAsync(cancellationToken);

        var allEvents = verificationEvents
            .Concat(creditEvents)
            .Concat(operatorNoteEvents)
            .Concat(auditEvents)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EventType))
            allEvents = allEvents.Where(e => e.EventType == request.EventType);

        var ordered = allEvents.OrderByDescending(e => e.OccurredAt).ToList();

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var totalCount = ordered.Count;

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<AuditLogEntryDto>(items, totalCount, page, pageSize);
    }
}
