using Infrastructure.Security.Authorization.Providers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using UseCases.Common.Security.Authentication.Contexts;

namespace Infrastructure.Security.Authorization.Requirements;

internal class HasAuthorizationRequirementHandler(IServiceProvider serviceProvider)
    : AuthorizationHandler<HasAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasAuthorizationRequirement requirement)
    {
        var userContext = serviceProvider.GetRequiredService<IUserContext>();
        var authorizationProvider = serviceProvider.GetRequiredService<IUserAuthorizationProvider>();

        if (!userContext.IsAuthenticated || userContext.UserId is null)
        {
            context.Fail(new AuthorizationFailureReason(this, "User not authenticated"));
            return;
        }

        var userAuthorization = await authorizationProvider.GetUserAuthorizationAsync(userContext.UserId.Value);
        if (userAuthorization is null)
        {
            context.Fail(new AuthorizationFailureReason(this, "User data not found"));
            return;
        }

        // ✅ Permissions
        if (requirement.Permissions.Length > 0)
        {
            var missingPermission = requirement.Permissions
                .FirstOrDefault(p => !userAuthorization.Permissions.Contains(p));
            if (missingPermission is not null)
            {
                context.Fail(new AuthorizationFailureReason(this, $"Missing permission: {missingPermission}"));
                return;
            }
        }

        // ✅ Policies
        if (requirement.Policies.Length > 0)
        {
            var missingPolicy = requirement.Policies
                .FirstOrDefault(p => !userAuthorization.Policies.Contains(p));
            if (missingPolicy is not null)
            {
                context.Fail(new AuthorizationFailureReason(this, $"Missing policy: {missingPolicy}"));
                return;
            }
        }

        // ✅ Roles
        if (requirement.Roles.Length > 0)
        {
            var missingRole = requirement.Roles
                .FirstOrDefault(r => !userAuthorization.Roles.Contains(r));
            if (missingRole is not null)
            {
                context.Fail(new AuthorizationFailureReason(this, $"Missing role: {missingRole}"));
                return;
            }
        }

        context.Succeed(requirement);
    }
}
