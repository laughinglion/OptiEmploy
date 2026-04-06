using EmploymentVerify.Application.Common;
using MediatR;

namespace EmploymentVerify.Application.Admin.Queries;

public record GetAuditLogQuery(
    string? EventType = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<AuditLogEntryDto>>;

public record AuditLogEntryDto(
    DateTime OccurredAt,
    string EventType,
    string Description,
    string? ActorEmail,
    string? RelatedEntityId);
