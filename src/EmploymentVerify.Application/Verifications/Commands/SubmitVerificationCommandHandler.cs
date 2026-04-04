using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using MediatR;

namespace EmploymentVerify.Application.Verifications.Commands;

public class SubmitVerificationCommandHandler : IRequestHandler<SubmitVerificationCommand, SubmitVerificationResult>
{
    private readonly IApplicationDbContext _context;

    public SubmitVerificationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SubmitVerificationResult> Handle(SubmitVerificationCommand request, CancellationToken cancellationToken)
    {
        // Parse enums from string values (already validated by FluentValidation)
        var idType = Enum.Parse<IdentificationType>(request.IdType);
        var consentType = Enum.Parse<ConsentType>(request.ConsentType);

        var verification = new VerificationRequest
        {
            Id = Guid.NewGuid(),
            RequestorId = request.RequestorId,
            EmployeeFullName = request.EmployeeFullName.Trim(),
            IdType = idType,
            SaIdNumber = idType == IdentificationType.SaIdNumber ? request.SaIdNumber?.Trim() : null,
            PassportNumber = idType == IdentificationType.Passport ? request.PassportNumber?.Trim() : null,
            PassportCountry = idType == IdentificationType.Passport ? request.PassportCountry?.Trim() : null,
            CompanyName = request.CompanyName.Trim(),
            CompanyId = request.SelectedCompanyId,
            JobTitle = request.JobTitle.Trim(),
            EmploymentStartDate = request.EmploymentStartDate,
            EmploymentEndDate = request.EmploymentEndDate,
            HrContactName = string.IsNullOrWhiteSpace(request.HrContactName) ? null : request.HrContactName.Trim(),
            HrEmail = string.IsNullOrWhiteSpace(request.HrEmail) ? null : request.HrEmail.Trim().ToLowerInvariant(),
            HrPhone = string.IsNullOrWhiteSpace(request.HrPhone) ? null : request.HrPhone.Trim(),

            // POPIA consent — recorded with timestamp for audit trail
            PopiaConsentGiven = request.ConsentToPopia,
            AccuracyConfirmed = request.ConsentAccuracy,
            ConsentType = consentType,
            ConsentRecordedAt = DateTime.UtcNow,

            Status = VerificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.VerificationRequests.Add(verification);
        await _context.SaveChangesAsync(cancellationToken);

        return new SubmitVerificationResult(
            verification.Id,
            verification.Status.ToString(),
            verification.CreatedAt
        );
    }
}
