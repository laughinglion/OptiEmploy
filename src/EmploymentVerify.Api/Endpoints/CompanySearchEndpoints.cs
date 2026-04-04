using EmploymentVerify.Api.Filters;
using EmploymentVerify.Application.Companies.Queries;
using EmploymentVerify.Domain.Constants;
using MediatR;

namespace EmploymentVerify.Api.Endpoints;

public static class CompanySearchEndpoints
{
    public static void MapCompanySearchEndpoints(this IEndpointRouteBuilder app)
    {
        // Company search endpoint accessible to Requestors (and Admins/Operators)
        // Used for autocomplete in the verification request form
        app.MapGet("/api/companies/search", async (
            string? q,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return Results.Ok(Array.Empty<CompanySearchResult>());
            }

            var query = new SearchCompaniesQuery(q);
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("SearchCompanies")
        .WithTags("Companies")
        .WithDescription("Search verified companies for autocomplete (requires any authenticated role)")
        .RequireAuthorization(AuthorizationPolicies.RequireAnyRole)
        .AddEndpointFilter(new RoleAuthorizationFilter(AppRoles.Admin, AppRoles.Requestor, AppRoles.Operator))
        .Produces<List<CompanySearchResult>>(StatusCodes.Status200OK);
    }
}
