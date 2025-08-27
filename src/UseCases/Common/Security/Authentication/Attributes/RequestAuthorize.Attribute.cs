using Microsoft.AspNetCore.Authorization;

using UseCases.Common.Security.Authorization.Claims;

namespace UseCases.Common.Security.Authentication.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequestAuthorizeAttribute : AuthorizeAttribute
{
    public RequestAuthorizeAttribute(string? permissions)
    {
        Permissions = permissions;
        Policy = BuildPolicy();
    }

    public RequestAuthorizeAttribute(string? permissions = null, string? policies = null, string? roles = null)
    {
        Permissions = permissions;
        Policies = policies;
        Roles = roles;
        Policy = BuildPolicy();
    }

    public string? Permissions { get; }
    public string? Policies { get; }

    private string BuildPolicy()
    {
        var policyParts = new List<string>();

        if (!string.IsNullOrEmpty(Permissions))
            policyParts.Add($"{CustomClaim.Permission}:{Permissions}");

        if (!string.IsNullOrEmpty(Policies))
            policyParts.Add($"{CustomClaim.Policy}:{Policies}");

        if (!string.IsNullOrEmpty(Roles))
            policyParts.Add($"{CustomClaim.Role}:{Roles}");

        return string.Join(";", policyParts);
    }
}