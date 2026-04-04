using MediatR;

namespace EmploymentVerify.Application.Verifications.Queries;

public record GetAllVerificationsQuery(string? StatusFilter = null) : IRequest<List<VerificationSummaryDto>>;
