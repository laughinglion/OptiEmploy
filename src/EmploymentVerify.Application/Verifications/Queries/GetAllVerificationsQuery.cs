using EmploymentVerify.Application.Common;
using MediatR;

namespace EmploymentVerify.Application.Verifications.Queries;

public record GetAllVerificationsQuery(
    string? StatusFilter = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<VerificationSummaryDto>>;
