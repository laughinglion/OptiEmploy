using MediatR;

namespace EmploymentVerify.Application.Verifications.Queries;

public record GetVerificationByTokenQuery(string Token) : IRequest<HrConfirmationResult?>;

public record HrConfirmationResult(
    Guid VerificationRequestId,
    string EmployeeFullName,
    string CompanyName,
    string JobTitle,
    DateOnly EmploymentStartDate,
    DateOnly? EmploymentEndDate);
