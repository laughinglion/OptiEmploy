using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Verifications.Queries;

public class GetVerificationDetailQueryHandler : IRequestHandler<GetVerificationDetailQuery, VerificationDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetVerificationDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VerificationDetailDto?> Handle(GetVerificationDetailQuery request, CancellationToken cancellationToken)
    {
        var verification = await _context.VerificationRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VerificationId, cancellationToken);

        if (verification is null)
            return null;

        if (!request.IsAdmin && verification.RequestorId != request.RequestorId)
            return null;

        var response = await _context.VerificationResponses
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.VerificationRequestId == request.VerificationId, cancellationToken);

        var employeeIdNumber = verification.SaIdNumber
            ?? verification.PassportNumber
            ?? string.Empty;

        VerificationResponseDto? responseDto = response is null ? null : new VerificationResponseDto(
            response.RespondedBy,
            response.ResponseType.ToString(),
            response.ConfirmedJobTitle,
            response.ConfirmedStartDate,
            response.ConfirmedEndDate,
            response.IsCurrentlyEmployed,
            response.Notes,
            response.RespondedAt);

        return new VerificationDetailDto(
            verification.Id,
            verification.RequestorId,
            verification.EmployeeFullName,
            employeeIdNumber,
            verification.CompanyName,
            verification.JobTitle,
            verification.EmploymentStartDate,
            verification.EmploymentEndDate,
            verification.Status.ToString(),
            verification.VerificationMethod?.ToString(),
            verification.CreatedAt,
            verification.CompletedAt,
            verification.ConsentType.ToString(),
            responseDto);
    }
}
