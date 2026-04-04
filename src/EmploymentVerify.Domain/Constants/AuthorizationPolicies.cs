namespace EmploymentVerify.Domain.Constants;

/// <summary>
/// Named authorization policy constants used throughout the application.
/// Policies are registered in the DI container and referenced by Blazor pages and components.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Only Admin users.</summary>
    public const string RequireAdmin = "RequireAdmin";

    /// <summary>Only Requestor users.</summary>
    public const string RequireRequestor = "RequireRequestor";

    /// <summary>Only Operator users.</summary>
    public const string RequireOperator = "RequireOperator";

    /// <summary>Admin or Operator — for viewing verification queues/details.</summary>
    public const string RequireAdminOrOperator = "RequireAdminOrOperator";

    /// <summary>Admin or Requestor — for viewing verification reports.</summary>
    public const string RequireAdminOrRequestor = "RequireAdminOrRequestor";

    /// <summary>Any authenticated user with a valid role (Admin, Requestor, or Operator).</summary>
    public const string RequireAnyRole = "RequireAnyRole";

    /// <summary>All named policies for enumeration and validation.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        RequireAdmin,
        RequireRequestor,
        RequireOperator,
        RequireAdminOrOperator,
        RequireAdminOrRequestor,
        RequireAnyRole
    ];
}
