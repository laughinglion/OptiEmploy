using MediatR;

namespace EmploymentVerify.Application.Verifications.Queries;

public record GetWorkQueueQuery : IRequest<List<WorkQueueItemDto>>;
