using EmploymentVerify.Application.Common;
using MediatR;

namespace EmploymentVerify.Application.Verifications.Queries;

public record GetWorkQueueQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<WorkQueueItemDto>>;
