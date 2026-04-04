using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Verifications.Queries;

public class ListMyVerificationsQueryHandler : IRequestHandler<ListMyVerificationsQuery, List<VerificationSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public ListMyVerificationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<VerificationSummaryDto>> Handle(ListMyVerificationsQuery request, CancellationToken cancellationToken)
    {
        var verifications = await _context.VerificationRequests
            .AsNoTracking()
            .Where(v => v.RequestorId == request.RequestorId)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VerificationSummaryDto(
                v.Id,
                v.EmployeeFullName,
                v.CompanyName,
                v.JobTitle,
                v.Status.ToString(),
                v.CreatedAt,
                v.CompletedAt))
            .ToListAsync(cancellationToken);

        return verifications;
    }
}
