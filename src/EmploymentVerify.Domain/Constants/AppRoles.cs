namespace EmploymentVerify.Domain.Constants;

/// <summary>
/// String constants for role names, used in authorization attributes and policies.
/// These must match the <see cref="Enums.UserRole"/> enum member names exactly.
/// </summary>
public static class AppRoles
{
    public const string Admin = nameof(Enums.UserRole.Admin);
    public const string Requestor = nameof(Enums.UserRole.Requestor);
    public const string Operator = nameof(Enums.UserRole.Operator);

    /// <summary>All valid role names.</summary>
    public static readonly IReadOnlyList<string> All = [Admin, Requestor, Operator];
}
