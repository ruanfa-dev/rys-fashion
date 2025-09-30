using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using UseCases.Common.Security.Authentication.Contexts;
using UseCases.Common.Security.Authorization.Providers;

namespace Infrastructure.Security.Authorization.Requirements;

/// <summary>
/// Authorization handler that validates user permissions, roles, and policies.
/// Provides detailed logging and comprehensive authorization logic.
/// </summary>
internal class HasAuthorizationRequirementHandler(
    IServiceProvider serviceProvider,
    ILogger<HasAuthorizationRequirementHandler> logger)
    : AuthorizationHandler<HasAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasAuthorizationRequirement requirement)
    {
        try
        {
            IUserContext userContext = serviceProvider.GetRequiredService<IUserContext>();
            
            // Check if user is authenticated
            if (!userContext.IsAuthenticated || userContext.UserId is null)
            {
                logger.LogWarning("Authorization failed: User not authenticated");
                context.Fail(new AuthorizationFailureReason(this, "User not authenticated"));
                return;
            }

            Guid userId = userContext.UserId.Value;
            logger.LogDebug("Evaluating authorization for user {UserId}", userId);

            IUserAuthorizationProvider authorizationProvider = serviceProvider.GetRequiredService<IUserAuthorizationProvider>();
            UserAuthorizationData? userAuthorization = await authorizationProvider.GetUserAuthorizationAsync(userId);
            
            if (userAuthorization is null)
            {
                logger.LogWarning("Authorization failed: User data not found for user {UserId}", userId);
                context.Fail(new AuthorizationFailureReason(this, "User authorization data not found"));
                return;
            }

            // Validate permissions
            if (!ValidatePermissions(requirement, userAuthorization, userId))
            {
                context.Fail(new AuthorizationFailureReason(this, "Insufficient permissions"));
                return;
            }

            // Validate policies  
            if (!ValidatePolicies(requirement, userAuthorization, userId))
            {
                context.Fail(new AuthorizationFailureReason(this, "Policy requirements not met"));
                return;
            }

            // Validate roles
            if (!ValidateRoles(requirement, userAuthorization, userId))
            {
                context.Fail(new AuthorizationFailureReason(this, "Role requirements not met"));
                return;
            }

            logger.LogDebug("Authorization succeeded for user {UserId}", userId);
            context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during authorization evaluation");
            context.Fail(new AuthorizationFailureReason(this, "Authorization evaluation failed"));
        }
    }

    /// <summary>
    /// Validates that the user has all required permissions.
    /// </summary>
    /// <param name="requirement">Authorization requirement</param>
    /// <param name="userAuthorization">User authorization data</param>
    /// <param name="userId">User ID for logging</param>
    /// <returns>True if all permissions are satisfied</returns>
    private bool ValidatePermissions(
        HasAuthorizationRequirement requirement, 
        UserAuthorizationData userAuthorization, 
        Guid userId)
    {
        if (requirement.Permissions.Length == 0)
            return true;

        logger.LogDebug("Checking permissions {RequiredPermissions} for user {UserId}", 
            requirement.Permissions, userId);

        foreach (string requiredPermission in requirement.Permissions)
        {
            if (!userAuthorization.Permissions.Contains(requiredPermission))
            {
                logger.LogWarning("User {UserId} missing required permission: {Permission}", 
                    userId, requiredPermission);
                return false;
            }
        }

        logger.LogDebug("All permission requirements satisfied for user {UserId}", userId);
        return true;
    }

    /// <summary>
    /// Validates that the user satisfies all required policies.
    /// </summary>
    /// <param name="requirement">Authorization requirement</param>
    /// <param name="userAuthorization">User authorization data</param>
    /// <param name="userId">User ID for logging</param>
    /// <returns>True if all policies are satisfied</returns>
    private bool ValidatePolicies(
        HasAuthorizationRequirement requirement, 
        UserAuthorizationData userAuthorization, 
        Guid userId)
    {
        if (requirement.Policies.Length == 0)
            return true;

        logger.LogDebug("Checking policies {RequiredPolicies} for user {UserId}", 
            requirement.Policies, userId);

        foreach (string requiredPolicy in requirement.Policies)
        {
            if (!userAuthorization.Policies.Contains(requiredPolicy))
            {
                logger.LogWarning("User {UserId} does not satisfy required policy: {Policy}", 
                    userId, requiredPolicy);
                return false;
            }
        }

        logger.LogDebug("All policy requirements satisfied for user {UserId}", userId);
        return true;
    }

    /// <summary>
    /// Validates that the user has all required roles.
    /// </summary>
    /// <param name="requirement">Authorization requirement</param>
    /// <param name="userAuthorization">User authorization data</param>
    /// <param name="userId">User ID for logging</param>
    /// <returns>True if all roles are satisfied</returns>
    private bool ValidateRoles(
        HasAuthorizationRequirement requirement, 
        UserAuthorizationData userAuthorization, 
        Guid userId)
    {
        if (requirement.Roles.Length == 0)
            return true;

        logger.LogDebug("Checking roles {RequiredRoles} for user {UserId}", 
            requirement.Roles, userId);

        foreach (string requiredRole in requirement.Roles)
        {
            if (!userAuthorization.Roles.Contains(requiredRole))
            {
                logger.LogWarning("User {UserId} missing required role: {Role}", 
                    userId, requiredRole);
                return false;
            }
        }

        logger.LogDebug("All role requirements satisfied for user {UserId}", userId);
        return true;
    }
}
