using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Verifications.Queries;

public class GetVerificationStatsQueryHandler : IRequestHandler<GetVerificationStatsQuery, VerificationStatsDto>
{
    private readonly IApplicationDbContext _context;

    public GetVerificationStatsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VerificationStatsDto> Handle(GetVerificationStatsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var all = await _context.VerificationRequests
            .AsNoTracking()
            .Select(v => new { v.Status, v.CreatedAt })
            .ToListAsync(cancellationToken);

        var totalToday = all.Count(v => v.CreatedAt >= todayStart);
        var totalThisMonth = all.Count(v => v.CreatedAt >= monthStart);
        var confirmedCount = all.Count(v => v.Status == VerificationStatus.Confirmed);
        var deniedCount = all.Count(v => v.Status == VerificationStatus.Denied);
        var pendingCount = all.Count(v => v.Status == VerificationStatus.Pending || v.Status == VerificationStatus.InProgress);

        var completedCount = confirmedCount + deniedCount;
        var confirmationRate = completedCount > 0
            ? Math.Round((decimal)confirmedCount / completedCount * 100, 2)
            : 0m;

        return new VerificationStatsDto(
            totalToday,
            totalThisMonth,
            confirmedCount,
            deniedCount,
            pendingCount,
            confirmationRate);
    }
}
