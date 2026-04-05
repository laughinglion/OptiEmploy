using System.Security.Claims;
using EmploymentVerify.Api.Filters;
using EmploymentVerify.Application.Verifications.Commands;
using EmploymentVerify.Application.Verifications.Queries;
using EmploymentVerify.Domain.Constants;
using EmploymentVerify.Domain.Enums;
using MediatR;
using SubmitVerificationResult = EmploymentVerify.Application.Verifications.Commands.SubmitVerificationResult;

namespace EmploymentVerify.Api.Endpoints;

public static class VerificationEndpoints
{
    public static void MapVerificationEndpoints(this IEndpointRouteBuilder app)
    {
        // ── Requestor endpoints ──────────────────────────────────────────────────

        var requestorGroup = app.MapGroup("/api/verifications")
            .WithTags("Verifications")
            .RequireAuthorization(AuthorizationPolicies.RequireAnyRole);

        // POST /api/verifications — submit a new verification request
        requestorGroup.MapPost("/", async (
            SubmitVerificationRequest request,
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";

            var command = new SubmitVerificationCommand(
                userId,
                request.EmployeeFullName,
                request.IdType,
                request.SaIdNumber,
                request.PassportNumber,
                request.PassportCountry,
                request.CompanyName,
                request.SelectedCompanyId,
                request.JobTitle,
                request.EmploymentStartDate,
                request.EmploymentEndDate,
                request.HrContactName,
                request.HrEmail,
                request.HrPhone,
                request.ConsentToPopia,
                request.ConsentAccuracy,
                request.ConsentType,
                baseUrl);

            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/verifications/{result.Id}", result);
        })
        .AddEndpointFilter(new RoleAuthorizationFilter(AppRoles.Requestor, AppRoles.Admin))
        .WithName("SubmitVerification")
        .WithDescription("Submit a new employment verification request")
        .Produces<SubmitVerificationResult>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized);

        // GET /api/verifications/my — list current user's verifications
        requestorGroup.MapGet("/my", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            var query = new ListMyVerificationsQuery(userId);
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .AddEndpointFilter(new RoleAuthorizationFilter(AppRoles.Requestor, AppRoles.Admin))
        .WithName("ListMyVerifications")
        .WithDescription("List all verifications submitted by the current user")
        .Produces<List<VerificationSummaryDto>>(StatusCodes.Status200OK);

        // GET /api/verifications/{id} — get verification detail
        requestorGroup.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            var roleValue = context.User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdmin = string.Equals(roleValue, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
                       || string.Equals(roleValue, AppRoles.Operator, StringComparison.OrdinalIgnoreCase);

            var query = new GetVerificationDetailQuery(id, userId, isAdmin);
            var result = await mediator.Send(query, cancellationToken);

            return result is null
                ? Results.NotFound(new { error = $"Verification with ID '{id}' was not found." })
                : Results.Ok(result);
        })
        .AddEndpointFilter(new RoleAuthorizationFilter(AppRoles.Requestor, AppRoles.Admin, AppRoles.Operator))
        .WithName("GetVerificationDetail")
        .WithDescription("Get details of a specific verification request")
        .Produces<VerificationDetailDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // GET /api/verifications/work-queue — operator/admin work queue
        requestorGroup.MapGet("/work-queue", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetWorkQueueQuery();
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .AddEndpointFilter(new RoleAuthorizationFilter(AppRoles.Admin, AppRoles.Operator))
        .WithName("GetWorkQueue")
        .WithDescription("Get the pending and in-progress verification work queue (Operator/Admin only)")
        .Produces<List<WorkQueueItemDto>>(StatusCodes.Status200OK);

        // GET /api/verifications — admin: get all verifications
        requestorGroup.MapGet("/", async (
            string? status,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAllVerificationsQuery(status);
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .AddEndpointFilter(new RoleAuthorizationFilter(AppRoles.Admin))
        .WithName("GetAllVerifications")
        .WithDescription("Get all verification requests with optional status filter (Admin only)")
        .Produces<List<VerificationSummaryDto>>(StatusCodes.Status200OK);

        // GET /api/verifications/stats — admin: aggregated stats
        requestorGroup.MapGet("/stats", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetVerificationStatsQuery();
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .AddEndpointFilter(new RoleAuthorizationFilter(AppRoles.Admin))
        .WithName("GetVerificationStats")
        .WithDescription("Get aggregated verification statistics (Admin only)")
        .Produces<VerificationStatsDto>(StatusCodes.Status200OK);

        // POST /api/verifications/{id}/operator-call — record operator phone call result
        requestorGroup.MapPost("/{id:guid}/operator-call", async (
            Guid id,
            OperatorCallRequest request,
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var operatorIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (operatorIdStr is null || !Guid.TryParse(operatorIdStr, out var operatorId))
                return Results.Unauthorized();

            if (!Enum.TryParse<CallOutcome>(request.CallOutcome, ignoreCase: true, out var callOutcome))
                return Results.BadRequest(new { error = $"Invalid CallOutcome value: '{request.CallOutcome}'." });

            var command = new RecordOperatorCallCommand(
                id,
                operatorId,
                callOutcome,
                request.Notes,
                request.ConfirmedJobTitle,
                request.ConfirmedStartDate,
                request.ConfirmedEndDate,
                request.IsCurrentlyEmployed,
                request.ResponseNotes);

            var result = await mediator.Send(command, cancellationToken);

            return result
                ? Results.Ok(new { message = "Call outcome recorded successfully." })
                : Results.NotFound(new { error = $"Verification with ID '{id}' was not found." });
        })
        .AddEndpointFilter(new RoleAuthorizationFilter(AppRoles.Admin, AppRoles.Operator))
        .WithName("RecordOperatorCall")
        .WithDescription("Record the outcome of an operator phone call for a verification (Operator/Admin only)")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // ── Public HR confirmation endpoints ────────────────────────────────────

        var publicGroup = app.MapGroup("/api/verify")
            .WithTags("HR Verification")
            .AllowAnonymous();

        // GET /api/verify/confirm?token=xxx — return HR confirmation page data
        publicGroup.MapGet("/confirm", async (
            string token,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            // Look up the token to return relevant request data for display
            var query = new GetVerificationByTokenQuery(token);
            var result = await mediator.Send(query, cancellationToken);

            return result is null
                ? Results.NotFound(new { error = "The verification link is invalid or has expired." })
                : Results.Ok(result);
        })
        .WithName("GetHrConfirmationData")
        .WithDescription("Get employment verification details for HR to review (public, token-secured)")
        .Produces<HrConfirmationResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/verify/confirm — submit HR response
        publicGroup.MapPost("/confirm", async (
            HrConfirmRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<ResponseType>(request.ResponseType, ignoreCase: true, out var responseType))
                return Results.BadRequest(new { error = $"Invalid ResponseType value: '{request.ResponseType}'." });

            var command = new RecordHrResponseCommand(
                request.Token,
                request.RespondedBy,
                responseType,
                request.ConfirmedJobTitle,
                request.ConfirmedStartDate,
                request.ConfirmedEndDate,
                request.IsCurrentlyEmployed,
                request.Notes);

            var result = await mediator.Send(command, cancellationToken);

            return result
                ? Results.Ok(new { message = "Thank you. Your response has been recorded." })
                : Results.BadRequest(new { error = "The verification link is invalid, has already been used, or has expired." });
        })
        .WithName("SubmitHrConfirmation")
        .WithDescription("Submit HR response to an employment verification request (public, token-secured)")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }
}

// ── Request / Response DTOs for this endpoint file ──────────────────────────

public record SubmitVerificationRequest(
    string EmployeeFullName,
    string IdType,
    string? SaIdNumber,
    string? PassportNumber,
    string? PassportCountry,
    string CompanyName,
    Guid? SelectedCompanyId,
    string JobTitle,
    DateOnly EmploymentStartDate,
    DateOnly? EmploymentEndDate,
    string? HrContactName,
    string? HrEmail,
    string? HrPhone,
    bool ConsentToPopia,
    bool ConsentAccuracy,
    string ConsentType);

public record OperatorCallRequest(
    string CallOutcome,
    string Notes,
    string? ConfirmedJobTitle,
    DateOnly? ConfirmedStartDate,
    DateOnly? ConfirmedEndDate,
    bool? IsCurrentlyEmployed,
    string? ResponseNotes);

public record HrConfirmRequest(
    string Token,
    string RespondedBy,
    string ResponseType,
    string? ConfirmedJobTitle,
    DateOnly? ConfirmedStartDate,
    DateOnly? ConfirmedEndDate,
    bool? IsCurrentlyEmployed,
    string? Notes);

public record HrConfirmationDto(
    Guid VerificationRequestId,
    string EmployeeFullName,
    string CompanyName,
    string JobTitle,
    DateOnly EmploymentStartDate,
    DateOnly? EmploymentEndDate);
