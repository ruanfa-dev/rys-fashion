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

    private static (List<string> permissions, List<string> policies, List<string> roles) ParsePolicyName(string policyName)
    {
        var permissions = new List<string>();
        var policies = new List<string>();
        var roles = new List<string>();

        var policyParts = policyName.AsSpan();
        var separator = ';';

        while (!policyParts.IsEmpty)
        {
            var nextSeparator = policyParts.IndexOf(separator);
            var part = nextSeparator >= 0 ? policyParts[..nextSeparator] : policyParts;

            if (!part.IsEmpty)
            {
                ProcessPolicyPart(part.ToString(), permissions, policies, roles);
            }

            policyParts = nextSeparator >= 0 ? policyParts[(nextSeparator + 1)..] : ReadOnlySpan<char>.Empty;
        }

        return (permissions, policies, roles);
    }

    private static void ProcessPolicyPart(string part, List<string> permissions, List<string> policies, List<string> roles)
    {
        if (part.StartsWith(CustomClaim.Permission, StringComparison.OrdinalIgnoreCase))
        {
            var values = part.AsSpan(CustomClaim.Permission.Length);
            AddValues(values, permissions);
        }
        else if (part.StartsWith(CustomClaim.Policy, StringComparison.OrdinalIgnoreCase))
        {
            var values = part.AsSpan(CustomClaim.Policy.Length);
            AddValues(values, policies);
        }
        else if (part.StartsWith(CustomClaim.Role, StringComparison.OrdinalIgnoreCase))
        {
            var values = part.AsSpan(CustomClaim.Role.Length);
            AddValues(values, roles);
        }
    }

    private static void AddValues(ReadOnlySpan<char> values, List<string> targetList)
    {
        if (values.IsEmpty) return;

        var separator = ';';
        while (!values.IsEmpty)
        {
            var nextSeparator = values.IndexOf(separator);
            var value = nextSeparator >= 0 ? values[..nextSeparator] : values;

            if (!value.IsEmpty)
            {
                targetList.Add(value.ToString());
            }

            values = nextSeparator >= 0 ? values[(nextSeparator + 1)..] : ReadOnlySpan<char>.Empty;
        }
    }
}
