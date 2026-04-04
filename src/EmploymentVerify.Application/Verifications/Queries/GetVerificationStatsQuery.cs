using MediatR;

namespace EmploymentVerify.Application.Verifications.Queries;

public record GetVerificationStatsQuery : IRequest<VerificationStatsDto>;
