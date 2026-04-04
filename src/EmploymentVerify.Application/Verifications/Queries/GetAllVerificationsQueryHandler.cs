using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Verifications.Queries;

public class GetAllVerificationsQueryHandler : IRequestHandler<GetAllVerificationsQuery, List<VerificationSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllVerificationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<VerificationSummaryDto>> Handle(GetAllVerificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.VerificationRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
            Enum.TryParse<VerificationStatus>(request.StatusFilter, ignoreCase: true, out var status))
        {
            query = query.Where(v => v.Status == status);
        }

        var verifications = await query
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
