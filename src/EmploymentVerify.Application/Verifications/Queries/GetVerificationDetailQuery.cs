using MediatR;

namespace EmploymentVerify.Application.Verifications.Queries;

public record GetVerificationDetailQuery(Guid VerificationId, Guid RequestorId, bool IsAdmin = false) : IRequest<VerificationDetailDto?>;
