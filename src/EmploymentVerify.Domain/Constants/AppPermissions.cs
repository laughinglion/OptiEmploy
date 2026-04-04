namespace EmploymentVerify.Domain.Constants;

/// <summary>
/// Defines explicit permission mappings from application actions and routes to authorization policies.
/// This serves as the single source of truth for what each role can do in the system.
///
/// Role summary:
///   Admin     — Full platform management: companies, users, credits, all verifications, statistics.
///   Requestor — Submit verifications, view own verification status/history, manage own profile.
///   Operator  — Process phone verification queue, record call outcomes, add notes.
/// </summary>
public static class AppPermissions
{
    // ───────────────────────────────────────────────
    // Company Directory Management (Admin only)
    // ───────────────────────────────────────────────

    /// <summary>Create a new company in the verified directory.</summary>
    public const string CompanyCreate = AuthorizationPolicies.RequireAdmin;

    /// <summary>Edit an existing company record.</summary>
    public const string CompanyEdit = AuthorizationPolicies.RequireAdmin;

    /// <summary>Delete/deactivate a company from the directory.</summary>
    public const string CompanyDelete = AuthorizationPolicies.RequireAdmin;

    /// <summary>View the company directory listing.</summary>
    public const string CompanyView = AuthorizationPolicies.RequireAdmin;

    /// <summary>Toggle the force-call flag on a company.</summary>
    public const string CompanyToggleForceCall = AuthorizationPolicies.RequireAdmin;

    // ───────────────────────────────────────────────
    // User Management (Admin only)
    // ───────────────────────────────────────────────

    /// <summary>Create a new user account.</summary>
    public const string UserCreate = AuthorizationPolicies.RequireAdmin;

    /// <summary>Deactivate/reactivate a user account.</summary>
    public const string UserDeactivate = AuthorizationPolicies.RequireAdmin;

    /// <summary>Assign or change a user's role.</summary>
    public const string UserAssignRole = AuthorizationPolicies.RequireAdmin;

    /// <summary>View user listing and details.</summary>
    public const string UserManage = AuthorizationPolicies.RequireAdmin;

    // ───────────────────────────────────────────────
    // Credits / Billing (Admin only)
    // ───────────────────────────────────────────────

    /// <summary>Add credits to a requestor's account.</summary>
    public const string CreditAdd = AuthorizationPolicies.RequireAdmin;

    /// <summary>View credit balances and transaction history.</summary>
    public const string CreditView = AuthorizationPolicies.RequireAdmin;

    // ───────────────────────────────────────────────
    // Verification Submission (Requestor only)
    // ───────────────────────────────────────────────

    /// <summary>Submit a new employment verification request.</summary>
    public const string VerificationSubmit = AuthorizationPolicies.RequireRequestor;

    /// <summary>View own verification history.</summary>
    public const string VerificationViewOwn = AuthorizationPolicies.RequireRequestor;

    // ───────────────────────────────────────────────
    // Verification Results (Admin + Requestor)
    // ───────────────────────────────────────────────

    /// <summary>View a completed verification report.</summary>
    public const string VerificationViewReport = AuthorizationPolicies.RequireAdminOrRequestor;

    /// <summary>View verification status (Requestor sees own, Admin sees all).</summary>
    public const string VerificationViewStatus = AuthorizationPolicies.RequireAdminOrRequestor;

    // ───────────────────────────────────────────────
    // Verification Queue (Admin + Operator)
    // ───────────────────────────────────────────────

    /// <summary>View the operator work queue of pending phone verifications.</summary>
    public const string OperatorQueueView = AuthorizationPolicies.RequireAdminOrOperator;

    /// <summary>Record a phone call outcome on a verification.</summary>
    public const string OperatorRecordOutcome = AuthorizationPolicies.RequireAdminOrOperator;

    /// <summary>Update verification status from the operator queue.</summary>
    public const string OperatorUpdateStatus = AuthorizationPolicies.RequireAdminOrOperator;

    /// <summary>Add notes to a verification request.</summary>
    public const string OperatorAddNotes = AuthorizationPolicies.RequireAdminOrOperator;

    // ───────────────────────────────────────────────
    // Admin Overview
    // ───────────────────────────────────────────────

    /// <summary>View all verifications across all requestors.</summary>
    public const string VerificationViewAll = AuthorizationPolicies.RequireAdmin;

    /// <summary>View platform statistics (verifications per day, confirmation rates).</summary>
    public const string StatisticsView = AuthorizationPolicies.RequireAdmin;

    // ───────────────────────────────────────────────
    // Shared / Profile
    // ───────────────────────────────────────────────

    /// <summary>View and update own profile. Available to any authenticated user with a role.</summary>
    public const string ProfileManage = AuthorizationPolicies.RequireAnyRole;

    // ───────────────────────────────────────────────
    // Route → Policy Mapping (for reference / programmatic use)
    // ───────────────────────────────────────────────

    /// <summary>
    /// Maps Blazor page routes to their required authorization policy.
    /// Used for documentation and can be consumed by middleware or navigation filtering.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> RoutePermissions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Admin pages
            ["/admin/companies"] = AuthorizationPolicies.RequireAdmin,
            ["/admin/companies/create"] = AuthorizationPolicies.RequireAdmin,
            ["/admin/companies/edit"] = AuthorizationPolicies.RequireAdmin,
            ["/admin/users"] = AuthorizationPolicies.RequireAdmin,
            ["/admin/users/create"] = AuthorizationPolicies.RequireAdmin,
            ["/admin/credits"] = AuthorizationPolicies.RequireAdmin,
            ["/admin/verifications"] = AuthorizationPolicies.RequireAdmin,
            ["/admin/statistics"] = AuthorizationPolicies.RequireAdmin,

            // Requestor pages
            ["/verifications/submit"] = AuthorizationPolicies.RequireRequestor,
            ["/verifications/history"] = AuthorizationPolicies.RequireRequestor,

            // Requestor + Admin pages
            ["/verifications/status"] = AuthorizationPolicies.RequireAdminOrRequestor,
            ["/verifications/report"] = AuthorizationPolicies.RequireAdminOrRequestor,

            // Operator pages (also accessible by Admin)
            ["/operator/queue"] = AuthorizationPolicies.RequireAdminOrOperator,
            ["/operator/verify"] = AuthorizationPolicies.RequireAdminOrOperator,

            // Shared pages
            ["/profile"] = AuthorizationPolicies.RequireAnyRole,
            ["/dashboard"] = AuthorizationPolicies.RequireAnyRole,
        };
}
