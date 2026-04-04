using EmploymentVerify.Api.Filters;
using EmploymentVerify.Application.Companies.Commands;
using EmploymentVerify.Application.Companies.Queries;
using EmploymentVerify.Domain.Constants;
using FluentValidation;
using MediatR;

namespace EmploymentVerify.Api.Endpoints;

public static class CompanyEndpoints
{
    public static void MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/companies")
            .WithTags("Companies")
            .RequireAuthorization(AuthorizationPolicies.RequireAdmin)
            .AddEndpointFilter(new RoleAuthorizationFilter(AppRoles.Admin));

        // List companies with optional search and filtering
        group.MapGet("/", async (
            string? search,
            bool? includeInactive,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new ListCompaniesQuery(
                SearchTerm: search,
                IncludeInactive: includeInactive ?? false);

            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("ListCompanies")
        .WithDescription("List all companies in the verified directory with optional search and filtering")
        .Produces<List<CompanyDto>>(StatusCodes.Status200OK);

        // Get a single company by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCompanyByIdQuery(id);
            var result = await mediator.Send(query, cancellationToken);

            return result is null
                ? Results.NotFound(new { error = $"Company with ID '{id}' was not found." })
                : Results.Ok(result);
        })
        .WithName("GetCompanyById")
        .WithDescription("Get a specific company by its ID")
        .Produces<CompanyDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Create a new company
        group.MapPost("/", async (
            CreateCompanyRequest request,
            IValidator<CreateCompanyCommand> validator,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateCompanyCommand(
                request.Name,
                request.RegistrationNumber,
                request.HrContactName,
                request.HrEmail,
                request.HrPhone,
                request.Address,
                request.City,
                request.Province,
                request.PostalCode,
                request.ForceCall);

            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(
                    validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            try
            {
                var result = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/companies/{result.Id}", result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .WithName("CreateCompany")
        .WithDescription("Add a new company to the verified company directory")
        .Produces<CompanyDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status409Conflict);

        // Update an existing company
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCompanyRequest request,
            IValidator<UpdateCompanyCommand> validator,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateCompanyCommand(
                id,
                request.Name,
                request.RegistrationNumber,
                request.HrContactName,
                request.HrEmail,
                request.HrPhone,
                request.Address,
                request.City,
                request.Province,
                request.PostalCode,
                request.ForceCall,
                request.IsActive);

            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(
                    validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            try
            {
                var result = await mediator.Send(command, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .WithName("UpdateCompany")
        .WithDescription("Update an existing company in the verified directory")
        .Produces<CompanyDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        // Soft-delete a company
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new DeleteCompanyCommand(id);
                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("DeleteCompany")
        .WithDescription("Soft-delete a company from the verified directory (marks as inactive)")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // Toggle force-call flag on a company (Admin only)
        group.MapPatch("/{id:guid}/force-call", async (
            Guid id,
            ToggleForceCallRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new ToggleForceCallCommand(id, request.ForceCall);
                var result = await mediator.Send(command, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("ToggleForceCall")
        .WithDescription("Flag or unflag a company as force-call only (skip email, always phone)")
        .Produces<ToggleForceCallResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}

public record CreateCompanyRequest(
    string Name,
    string RegistrationNumber,
    string HrContactName,
    string HrEmail,
    string HrPhone,
    string? Address,
    string? City,
    string? Province,
    string? PostalCode,
    bool ForceCall = false);

public record UpdateCompanyRequest(
    string Name,
    string RegistrationNumber,
    string HrContactName,
    string HrEmail,
    string HrPhone,
    string? Address,
    string? City,
    string? Province,
    string? PostalCode,
    bool ForceCall = false,
    bool IsActive = true);

public record ToggleForceCallRequest(bool ForceCall);
