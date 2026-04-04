using MediatR;

namespace EmploymentVerify.Application.Verifications.Queries;

public record ListMyVerificationsQuery(Guid RequestorId) : IRequest<List<VerificationSummaryDto>>;
