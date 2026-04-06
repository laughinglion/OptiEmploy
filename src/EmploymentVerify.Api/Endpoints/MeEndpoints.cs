using System.Security.Claims;
using EmploymentVerify.Application.Users.Commands;
using EmploymentVerify.Application.Users.Queries;
using EmploymentVerify.Domain.Constants;
using FluentValidation;
using MediatR;

namespace EmploymentVerify.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/me")
            .WithTags("My Account")
            .RequireAuthorization(AuthorizationPolicies.RequireAnyRole);

        // GET /api/me — current user's profile
        group.MapGet("/", async (HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyProfileQuery(userId.Value), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetMyProfile")
        .WithDescription("Get the current user's profile")
        .Produces<MyProfileDto>(StatusCodes.Status200OK);

        // PUT /api/me/profile — update name / company / phone
        group.MapPut("/profile", async (UpdateProfileRequest req, HttpContext ctx, IValidator<UpdateProfileCommand> validator, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            var command = new UpdateProfileCommand(userId.Value, req.FullName, req.CompanyName, req.PhoneNumber);
            var validationResult = await validator.ValidateAsync(command, ct);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("UpdateMyProfile")
        .WithDescription("Update the current user's display name, company and phone number")
        .Produces<UpdateProfileResult>(StatusCodes.Status200OK);

        // PUT /api/me/password — change password
        group.MapPut("/password", async (ChangePasswordRequest req, HttpContext ctx, IValidator<ChangePasswordCommand> validator, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            var command = new ChangePasswordCommand(userId.Value, req.CurrentPassword, req.NewPassword);
            var validationResult = await validator.ValidateAsync(command, ct);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var result = await mediator.Send(command, ct);
            return result.Success
                ? Results.Ok(new { message = "Password changed successfully." })
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("ChangeMyPassword")
        .WithDescription("Change the current user's password")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/me/data-export — POPIA right of access
        group.MapGet("/data-export", async (HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            var result = await mediator.Send(new ExportMyDataQuery(userId.Value), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("ExportMyData")
        .WithDescription("Export all personal data held for this account (POPIA right of access)")
        .Produces<MyDataExportDto>(StatusCodes.Status200OK);

        // DELETE /api/me/account — POPIA right to erasure
        group.MapDelete("/account", async (DeleteAccountRequest req, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            var result = await mediator.Send(new DeleteMyAccountCommand(userId.Value, req.Password), ct);
            return result.Success
                ? Results.Ok(new { message = "Your account has been anonymised in accordance with your POPIA right to erasure." })
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("DeleteMyAccount")
        .WithDescription("Anonymise and deactivate the current user's account (POPIA right to erasure)")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }

    private static Guid? GetUserId(HttpContext ctx)
    {
        var value = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }
}

public record UpdateProfileRequest(string FullName, string? CompanyName, string? PhoneNumber);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record DeleteAccountRequest(string Password);
