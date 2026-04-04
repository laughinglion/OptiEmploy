using EmploymentVerify.Api.Filters;
using EmploymentVerify.Application.Auth.Commands;
using EmploymentVerify.Domain.Constants;
using EmploymentVerify.Domain.Enums;
using FluentValidation;
using MediatR;

namespace EmploymentVerify.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        // Public endpoints — registration and email confirmation do not require authentication
        group.MapPost("/register", async (
            RegisterUserRequest request,
            IValidator<RegisterUserCommand> validator,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterUserCommand(
                request.Email,
                request.Password,
                request.FullName,
                request.CompanyName,
                request.Role);

            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                return Results.ValidationProblem(errors);
            }

            try
            {
                var result = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/users/{result.UserId}", new RegisterUserResponse(
                    result.UserId,
                    result.Email,
                    result.FullName,
                    result.Role,
                    result.EmailVerificationRequired,
                    "A verification email has been sent. Please check your inbox to activate your account."));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .AllowAnonymous()
        .WithName("RegisterUser")
        .WithDescription("Register a new user with email and password, optionally specifying a role. A verification email is sent upon registration.")
        .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/confirm-email", async (
            string token,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new ConfirmEmailCommand(token);
            var result = await mediator.Send(command, cancellationToken);

            if (result.Success)
            {
                return Results.Ok(new ConfirmEmailResponse(result.UserId, result.Email, result.Message));
            }

            return Results.BadRequest(new { error = result.Message });
        })
        .AllowAnonymous()
        .WithName("ConfirmEmail")
        .WithDescription("Confirm a user's email address using the verification token sent during registration")
        .Produces<ConfirmEmailResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // Admin-only endpoints — protected by JWT authentication + Admin role guard
        var adminGroup = app.MapGroup("/api/admin/users")
            .WithTags("User Management")
            .RequireAuthorization(AuthorizationPolicies.RequireAdmin)
            .AddEndpointFilter(new RoleAuthorizationFilter(AppRoles.Admin));

        adminGroup.MapPut("/{userId:guid}/role", async (
            Guid userId,
            AssignUserRoleRequest request,
            IValidator<AssignUserRoleCommand> validator,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignUserRoleCommand(userId, request.Role);

            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                return Results.ValidationProblem(errors);
            }

            try
            {
                var result = await mediator.Send(command, cancellationToken);
                return Results.Ok(new AssignUserRoleResponse(
                    result.UserId,
                    result.Email,
                    result.PreviousRole,
                    result.NewRole));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("AssignUserRole")
        .WithDescription("Assign a role to an existing user (Admin only)")
        .Produces<AssignUserRoleResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}

public record RegisterUserRequest(
    string Email,
    string Password,
    string FullName,
    string? CompanyName,
    UserRole? Role = null);

public record RegisterUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    bool EmailVerificationRequired,
    string Message);

public record ConfirmEmailResponse(
    Guid UserId,
    string Email,
    string Message);

public record AssignUserRoleRequest(
    UserRole Role);

public record AssignUserRoleResponse(
    Guid UserId,
    string Email,
    string PreviousRole,
    string NewRole);
