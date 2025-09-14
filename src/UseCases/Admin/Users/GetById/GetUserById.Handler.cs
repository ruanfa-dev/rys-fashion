using Core.Identity;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Users.Common;
using UseCases.Common.Security.Authorization.Claims;

namespace UseCases.Admin.Users.GetById;

public static partial class GetUserById
{
    public sealed record Result : UserDetailedResult;

    public sealed record Query(Guid Id) : IQuery<Result>;

    public sealed class Handler(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILogger<Handler> logger
    ) : IQueryHandler<Query, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await userManager.FindByIdAsync(request.Id.ToString());
                if (user == null)
                {
                    return User.Errors.UserNotFound;
                }

                // Get: user roles
                var roles = await userManager.GetRolesAsync(user);

                // Collect: permissions assigned to roles (case-insensitive)
                var rolePermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (roles?.Count > 0)
                {
                    var roleNameSet = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);
                    var matchedRoles = await roleManager.Roles
                        .Where(r => roleNameSet.Contains(r.Name!))
                        .ToListAsync(cancellationToken);

                    foreach (var r in matchedRoles)
                    {
                        var claimList = await roleManager.GetClaimsAsync(r);
                        if (claimList == null) continue;

                        foreach (var perm in claimList
                                     .Where(c => string.Equals(c.Type, CustomClaim.Permission, StringComparison.OrdinalIgnoreCase))
                                     .Select(c => c.Value)
                                     .Where(v => !string.IsNullOrWhiteSpace(v)))
                        {
                            rolePermissions.Add(perm!);
                        }
                    }
                }

                // Get: user claims
                var userClaims = await userManager.GetClaimsAsync(user);

                // Build dictionary of first claim value per claim type (case-insensitive keys)
                var claimsDict = userClaims
                    .GroupBy(c => c.Type, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);

                // Extract user-level permissions from claims
                var userPermissions = userClaims
                    .Where(c => string.Equals(c.Type, CustomClaim.Permission, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Value!)
                    .Where(v => !string.IsNullOrWhiteSpace(v));

                rolePermissions.UnionWith(userPermissions);

                var result = new Result
                {
                    Id = user.Id,
                    Email = user.Email!,
                    UserName = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    ProfileImagePath = user.ProfileImagePath,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    CreatedBy = user.CreatedBy,
                    UpdatedAt = user.UpdatedAt,
                    UpdatedBy = user.UpdatedBy,
                    LastSignInAt = user.LastSignInAt,
                    CurrentSignInAt = user.CurrentSignInAt,
                    LastSignInIp = user.LastSignInIp,
                    CurrentSignInIp = user.CurrentSignInIp,
                    SignInCount = user.SignInCount,
                    Roles = roles?.ToArray(),
                    RolePermissions = rolePermissions?.ToArray(),
                    UserPermissions = userPermissions.ToArray(),
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                };

                logger.LogDebug("Retrieved user {UserId} with {RoleCount} roles, {RolePermissions} role-permissions and {UserPermissions}",
                    user.Id, roles?.Count, result.RolePermissions, result.UserPermissions);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving user {UserId}", request.Id);
                return Error.Failure("User.RetrievalFailed", "Failed to retrieve user details");
            }
        }
    }
}