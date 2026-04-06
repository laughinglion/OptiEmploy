using MediatR;

namespace EmploymentVerify.Application.Verifications.Queries;

public record ExportVerificationPdfQuery(Guid VerificationId, Guid RequestorId, bool IsAdmin) : IRequest<byte[]?>;
