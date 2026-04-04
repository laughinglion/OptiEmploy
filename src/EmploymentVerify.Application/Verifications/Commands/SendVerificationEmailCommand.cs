using MediatR;

namespace EmploymentVerify.Application.Verifications.Commands;

public record SendVerificationEmailCommand(Guid VerificationRequestId, string BaseUrl) : IRequest<bool>;
