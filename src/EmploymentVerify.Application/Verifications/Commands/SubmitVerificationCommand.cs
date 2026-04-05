using MediatR;

namespace EmploymentVerify.Application.Verifications.Commands;

/// <summary>
/// Command to submit a new employment verification request.
/// POPIA consent fields are mandatory — validation enforces this.
/// </summary>
public record SubmitVerificationCommand(
    Guid RequestorId,
    string EmployeeFullName,
    string IdType,
    string? SaIdNumber,
    string? PassportNumber,
    string? PassportCountry,
    string CompanyName,
    Guid? SelectedCompanyId,
    string JobTitle,
    DateOnly EmploymentStartDate,
    DateOnly? EmploymentEndDate,
    string? HrContactName,
    string? HrEmail,
    string? HrPhone,
    bool ConsentToPopia,
    bool ConsentAccuracy,
    string ConsentType,
    string? BaseUrl = null
) : IRequest<SubmitVerificationResult>;

public record SubmitVerificationResult(
    Guid Id,
    string Status,
    DateTime CreatedAt
);
