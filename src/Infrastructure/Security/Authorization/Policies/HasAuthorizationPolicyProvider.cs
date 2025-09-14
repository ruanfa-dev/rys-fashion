using Infrastructure.Security.Authorization.Requirements;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

using UseCases.Common.Security.Authorization.Claims;

namespace Infrastructure.Security.Authorization.Policies;

internal class HasAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly AuthorizationOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return Task.FromResult(_options.DefaultPolicy);
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return Task.FromResult(_options.FallbackPolicy);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
            throw new ArgumentException("Policy name cannot be null or empty.", nameof(policyName));

        var existingPolicy = _options.GetPolicy(policyName);
        if (existingPolicy != null)
        {
            return Task.FromResult<AuthorizationPolicy?>(existingPolicy);
        }

        var (permissions, policies, roles) = ParsePolicyName(policyName);

        if (permissions.Count == 0 && policies.Count == 0 && roles.Count == 0)
            return Task.FromResult<AuthorizationPolicy?>(null);

        var requirement = new HasAuthorizationRequirement(
            [.. permissions],
            [.. policies],
            [.. roles]);

        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(requirement)
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    /// <summary>
    /// Parses a policy name into its component permissions, policies, and roles.
    /// Format: "permission:perm1,perm2;policy:pol1,pol2;role:role1,role2"
    /// </summary>
    /// <param name="policyName">The policy name to parse</param>
    /// <returns>Tuple containing lists of permissions, policies, and roles</returns>
    private static (List<string> permissions, List<string> policies, List<string> roles) ParsePolicyName(string policyName)
    {
        var permissions = new List<string>();
        var policies = new List<string>();
        var roles = new List<string>();

        try
        {
            var policyParts = policyName.AsSpan();
            const char partSeparator = ';';

            while (!policyParts.IsEmpty)
            {
                var nextSeparator = policyParts.IndexOf(partSeparator);
                var part = nextSeparator >= 0 ? policyParts[..nextSeparator] : policyParts;

                if (!part.IsEmpty)
                {
                    ProcessPolicyPart(part.ToString(), permissions, policies, roles);
                }

                policyParts = nextSeparator >= 0 ? policyParts[(nextSeparator + 1)..] : ReadOnlySpan<char>.Empty;
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid policy format: {policyName}", nameof(policyName), ex);
        }

        return (permissions, policies, roles);
    }

    /// <summary>
    /// Processes a single policy part and adds values to the appropriate collection.
    /// </summary>
    /// <param name="part">Policy part to process</param>
    /// <param name="permissions">Collection to add permissions to</param>
    /// <param name="policies">Collection to add policies to</param>
    /// <param name="roles">Collection to add roles to</param>
    private static void ProcessPolicyPart(string part, List<string> permissions, List<string> policies, List<string> roles)
    {
        var colonIndex = part.IndexOf(':');
        if (colonIndex <= 0 || colonIndex >= part.Length - 1)
        {
            return; // Invalid format, skip this part
        }

        var claimType = part[..colonIndex];
        var valuesSpan = part.AsSpan(colonIndex + 1);

        if (string.Equals(claimType, CustomClaim.Permission, StringComparison.OrdinalIgnoreCase))
        {
            AddValuesToList(valuesSpan, permissions);
        }
        else if (string.Equals(claimType, CustomClaim.Policy, StringComparison.OrdinalIgnoreCase))
        {
            AddValuesToList(valuesSpan, policies);
        }
        else if (string.Equals(claimType, CustomClaim.Role, StringComparison.OrdinalIgnoreCase))
        {
            AddValuesToList(valuesSpan, roles);
        }
    }

    /// <summary>
    /// Adds comma-separated values to the target list.
    /// </summary>
    /// <param name="values">Span containing comma-separated values</param>
    /// <param name="targetList">List to add values to</param>
    private static void AddValuesToList(ReadOnlySpan<char> values, List<string> targetList)
    {
        if (values.IsEmpty) return;

        const char valueSeparator = ',';
        
        while (!values.IsEmpty)
        {
            var nextSeparator = values.IndexOf(valueSeparator);
            var value = nextSeparator >= 0 ? values[..nextSeparator] : values;

            if (!value.IsEmpty)
            {
                var trimmedValue = value.ToString().Trim();
                if (!string.IsNullOrEmpty(trimmedValue))
                {
                    targetList.Add(trimmedValue);
                }
            }

            values = nextSeparator >= 0 ? values[(nextSeparator + 1)..] : ReadOnlySpan<char>.Empty;
        }
    }
}
