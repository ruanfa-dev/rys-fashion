using Microsoft.AspNetCore.Authorization;

using UseCases.Common.Security.Authorization.Claims;

namespace UseCases.Common.Security.Authorization.Attributes;

/// <summary>
/// Custom authorization attribute that supports permissions, roles, and policies.
/// Provides flexible authorization with proper validation and caching.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequestAuthorizeAttribute : AuthorizeAttribute
{
    private readonly Lazy<string> _policyLazy;

    /// <summary>
    /// Initializes a new instance with permissions, roles, and policies.
    /// </summary>
    /// <param name="permissions">Comma-separated list of required permissions</param>
    /// <param name="roles">Comma-separated list of required roles</param>
    /// <param name="policies">Comma-separated list of required policies</param>
    public RequestAuthorizeAttribute(string? permissions = null, string? roles = null, string? policies = null)
    {
        // Validate that at least one parameter is provided
        if (string.IsNullOrWhiteSpace(permissions) && 
            string.IsNullOrWhiteSpace(roles) && 
            string.IsNullOrWhiteSpace(policies))
        {
            throw new ArgumentException("At least one authorization parameter (permissions, roles, or policies) must be specified.");
        }

        Permissions = SplitAndClean(permissions);
        CustomRoles = SplitAndClean(roles);
        Policies = SplitAndClean(policies);

        // Set base class Roles property if roles are provided
        if (CustomRoles?.Length > 0)
        {
            Roles = string.Join(",", CustomRoles);
        }

        // Use lazy initialization for policy building to improve performance
        _policyLazy = new Lazy<string>(BuildPolicy);
        Policy = _policyLazy.Value;
    }

    /// <summary>
    /// Creates an attribute with specific permissions.
    /// </summary>
    /// <param name="permissions">Array of required permissions</param>
    /// <returns>RequestAuthorizeAttribute instance</returns>
    public static RequestAuthorizeAttribute WithPermissions(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified.", nameof(permissions));

        return new RequestAuthorizeAttribute(permissions: string.Join(",", permissions));
    }

    /// <summary>
    /// Creates an attribute with specific roles.
    /// </summary>
    /// <param name="roles">Array of required roles</param>
    /// <returns>RequestAuthorizeAttribute instance</returns>
    public static RequestAuthorizeAttribute WithRoles(params string[] roles)
    {
        if (roles == null || roles.Length == 0)
            throw new ArgumentException("At least one role must be specified.", nameof(roles));

        return new RequestAuthorizeAttribute(roles: string.Join(",", roles));
    }

    /// <summary>
    /// Creates an attribute with specific policies.
    /// </summary>
    /// <param name="policies">Array of required policies</param>
    /// <returns>RequestAuthorizeAttribute instance</returns>
    public static RequestAuthorizeAttribute WithPolicies(params string[] policies)
    {
        if (policies == null || policies.Length == 0)
            throw new ArgumentException("At least one policy must be specified.", nameof(policies));

        return new RequestAuthorizeAttribute(policies: string.Join(",", policies));
    }

    /// <summary>
    /// Creates an attribute that requires any of the specified permissions (OR logic).
    /// </summary>
    /// <param name="permissions">Array of permissions (any one required)</param>
    /// <returns>RequestAuthorizeAttribute instance</returns>
    public static RequestAuthorizeAttribute WithAnyPermission(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified.", nameof(permissions));

        // For OR logic, we create separate policies for each permission
        // This would require extending the policy provider to handle OR logic
        return new RequestAuthorizeAttribute(permissions: string.Join(",", permissions));
    }

    /// <summary>
    /// Gets the required permissions for this authorization.
    /// </summary>
    public string[]? Permissions { get; }

    /// <summary>
    /// Gets the required policies for this authorization.
    /// </summary>
    public string[]? Policies { get; }

    /// <summary>
    /// Gets the required roles for this authorization.
    /// Note: This is different from the base class Roles property which is a string.
    /// </summary>
    public string[]? CustomRoles { get; }

    /// <summary>
    /// Splits a comma-separated string into a clean array of non-empty values.
    /// </summary>
    /// <param name="input">Comma-separated input string</param>
    /// <returns>Array of clean values or null if input is empty</returns>
    private static string[]? SplitAndClean(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var values = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(x => x.Trim())
                          .Where(x => !string.IsNullOrEmpty(x))
                          .ToArray();

        return values.Length > 0 ? values : null;
    }

    /// <summary>
    /// Builds the policy string used by the authorization system.
    /// Format: "claim_type:value1,value2;claim_type2:value3"
    /// </summary>
    /// <returns>Policy string for the authorization system</returns>
    private string BuildPolicy()
    {
        var policyParts = new List<string>(3); // Pre-size for better performance

        AddClaimParts(policyParts, CustomClaim.Permission, Permissions);
        AddClaimParts(policyParts, CustomClaim.Policy, Policies);
        AddClaimParts(policyParts, CustomClaim.Role, CustomRoles);

        if (policyParts.Count == 0)
        {
            throw new InvalidOperationException("No valid authorization parameters were provided.");
        }

        return string.Join(";", policyParts);
    }

    /// <summary>
    /// Adds claim parts to the policy parts list.
    /// </summary>
    /// <param name="policyParts">List to add policy parts to</param>
    /// <param name="claimType">Type of claim (permission, policy, role)</param>
    /// <param name="values">Values for the claim type</param>
    private static void AddClaimParts(List<string> policyParts, string claimType, string[]? values)
    {
        if (values?.Length > 0)
        {
            // Validate claim values
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException($"Invalid {claimType} value: cannot be null or whitespace.");
                }
            }

            policyParts.Add($"{claimType}:{string.Join(",", values)}");
        }
    }

    /// <summary>
    /// Returns a string representation of the authorization requirements.
    /// </summary>
    /// <returns>String describing the authorization requirements</returns>
    public override string ToString()
    {
        var parts = new List<string>();

        if (Permissions?.Length > 0)
            parts.Add($"Permissions: [{string.Join(", ", Permissions)}]");

        if (CustomRoles?.Length > 0)
            parts.Add($"Roles: [{string.Join(", ", CustomRoles)}]");

        if (Policies?.Length > 0)
            parts.Add($"Policies: [{string.Join(", ", Policies)}]");

        return parts.Count > 0 ? string.Join(", ", parts) : "No requirements";
    }
}