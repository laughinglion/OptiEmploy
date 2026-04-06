using EmploymentVerify.Api.Filters;
using EmploymentVerify.Application.Auth.Commands;
using EmploymentVerify.Application.Users.Commands;
using EmploymentVerify.Application.Users.Queries;
using CreditTransactionDto = EmploymentVerify.Application.Users.Queries.CreditTransactionDto;
using LoginCommand = EmploymentVerify.Application.Auth.Commands.LoginCommand;
using EmploymentVerify.Domain.Constants;
using EmploymentVerify.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace EmploymentVerify.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        // Public endpoints — registration and email confirmation do not require authentication
        // POST /api/auth/login — email/password authentication
        group.MapPost("/login", async (
            LoginRequest request,
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(request.Email, request.Password);
            var result = await mediator.Send(command, cancellationToken);

            if (!result.Success)
                return Results.Unauthorized();

            var loginResponse = new LoginResponse(result.Token!, result.UserId!.Value, result.Email!, result.FullName!, result.Role!);
            // Store refresh token in HttpOnly cookie
            context.Response.Cookies.Append("refresh_token", result.RefreshToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            return Results.Ok(loginResponse);
        })
        .AllowAnonymous()
        .RequireRateLimiting("auth")
        .WithName("Login")
        .WithDescription("Authenticate with email and password, returns a JWT access token")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        // POST /api/auth/sso — SSO identity linking (called by the Web app after OAuth)
        group.MapPost("/sso", async (
            SsoLoginRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return Results.BadRequest(new { error = "Email is required." });

            var command = new SsoLoginCommand(request.Email, request.FullName ?? request.Email, request.Provider ?? "External");
            var result = await mediator.Send(command, cancellationToken);

            if (!result.Success)
                return Results.Unauthorized();

            return Results.Ok(new LoginResponse(result.Token!, result.UserId!.Value, result.Email!, result.FullName!, result.Role!));
        })
        .AllowAnonymous()
        .WithName("SsoLogin")
        .WithDescription("Find or create a user from an external SSO provider, returns a JWT")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        // POST /api/auth/refresh — exchange refresh token for new access token
        group.MapPost("/refresh", async (
            RefreshRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new RefreshTokenCommand(request.RefreshToken);
            var result = await mediator.Send(command, cancellationToken);
            if (!result.Success)
                return Results.Unauthorized();
            return Results.Ok(new LoginResponse(result.Token!, result.UserId!.Value, result.Email!, result.FullName!, result.Role!));
        })
        .AllowAnonymous()
        .WithName("RefreshToken")
        .WithDescription("Exchange a refresh token for a new access token")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

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
        .RequireRateLimiting("auth")
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

        // GET /api/admin/users — list all users with optional role filter
        adminGroup.MapGet("/", async (
            string? role,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new ListUsersQuery(role);
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("ListUsers")
        .WithDescription("List all users, optionally filtered by role (Admin only)")
        .Produces<List<UserSummaryDto>>(StatusCodes.Status200OK);

        // PATCH /api/admin/users/{userId}/active — activate/deactivate a user
        adminGroup.MapPatch("/{userId:guid}/active", async (
            Guid userId,
            SetUserActiveRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new DeactivateUserCommand(userId, request.IsActive);
                var result = await mediator.Send(command, cancellationToken);
                return Results.Ok(new { result.UserId, result.Email, result.IsActive });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("SetUserActive")
        .WithDescription("Activate or deactivate a user account (Admin only)")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/admin/users/{userId}/credits — add credits to a user account
        adminGroup.MapPost("/{userId:guid}/credits", async (
            Guid userId,
            AddCreditsRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (request.Amount <= 0)
                return Results.BadRequest(new { error = "Amount must be greater than zero." });

            try
            {
                var command = new UpdateUserCreditCommand(userId, request.Amount, request.Reason ?? "Admin credit top-up");
                var newBalance = await mediator.Send(command, cancellationToken);
                return Results.Ok(new { UserId = userId, NewBalance = newBalance });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("AddUserCredits")
        .WithDescription("Add credits to a user's account (Admin only)")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // GET /api/admin/users/{userId}/transactions — credit transaction history
        adminGroup.MapGet("/{userId:guid}/transactions", async (
            Guid userId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserTransactionsQuery(userId);
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetUserTransactions")
        .WithDescription("Get credit transaction history for a user (Admin only)")
        .Produces<List<CreditTransactionDto>>(StatusCodes.Status200OK);

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

public record SetUserActiveRequest(bool IsActive);

public record AddCreditsRequest(decimal Amount, string? Reason);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, Guid UserId, string Email, string FullName, string Role);

public record SsoLoginRequest(string Email, string? FullName, string? Provider);

public record RefreshRequest(string RefreshToken);
