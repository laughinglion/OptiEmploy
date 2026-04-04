using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Verifications.Queries;

public class GetVerificationByTokenQueryHandler : IRequestHandler<GetVerificationByTokenQuery, HrConfirmationResult?>
{
    private readonly IApplicationDbContext _context;

    public GetVerificationByTokenQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HrConfirmationResult?> Handle(GetVerificationByTokenQuery request, CancellationToken cancellationToken)
    {
        var emailToken = await _context.EmailVerificationTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

        if (emailToken is null || emailToken.IsUsed || emailToken.ExpiresAt < DateTime.UtcNow)
            return null;

        var verification = await _context.VerificationRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == emailToken.VerificationRequestId, cancellationToken);

        if (verification is null)
            return null;

        return new HrConfirmationResult(
            verification.Id,
            verification.EmployeeFullName,
            verification.CompanyName,
            verification.JobTitle,
            verification.EmploymentStartDate,
            verification.EmploymentEndDate);
    }
}
