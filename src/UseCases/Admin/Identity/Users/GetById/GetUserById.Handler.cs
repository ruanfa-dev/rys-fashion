using System.Security.Claims;

using Core.Identity.Roles;
using Core.Identity.Users;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Identity.Users.Common;
using UseCases.Common.Security.Authorization.Claims;

namespace UseCases.Admin.Identity.Users.GetById;

public static partial class GetUserById
{
    public sealed record Result : UserResult.Detail;

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
                User? user = await userManager.FindByIdAsync(request.Id.ToString());
                if (user == null)
                {
                    return User.Errors.UserNotFound;
                }

                // Get: user roles
                IList<string>? roles = await userManager.GetRolesAsync(user);

                // Collect: permissions assigned to roles (case-insensitive)
                HashSet<string> rolePermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (roles?.Count > 0)
                {
                    HashSet<string> roleNameSet = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);
                    List<Role> matchedRoles = await roleManager.Roles
                        .Where(r => roleNameSet.Contains(r.Name!))
                        .ToListAsync(cancellationToken);

                    foreach (Role r in matchedRoles)
                    {
                        IList<Claim>? claimList = await roleManager.GetClaimsAsync(r);
                        if (claimList == null) continue;

                        foreach (string perm in claimList
                                     .Where(c => string.Equals(c.Type, CustomClaim.Permission, StringComparison.OrdinalIgnoreCase))
                                     .Select(c => c.Value)
                                     .Where(v => !string.IsNullOrWhiteSpace(v)))
                        {
                            rolePermissions.Add(perm!);
                        }
                    }
                }

                // Get: user claims
                IList<Claim> userClaims = await userManager.GetClaimsAsync(user);

                // Build dictionary of first claim value per claim type (case-insensitive keys)
                Dictionary<string, string> claimsDict = userClaims
                    .GroupBy(c => c.Type, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);

                // Extract user-level permissions from claims
                IEnumerable<string> userPermissions = userClaims
                    .Where(c => string.Equals(c.Type, CustomClaim.Permission, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Value!)
                    .Where(v => !string.IsNullOrWhiteSpace(v));

                rolePermissions.UnionWith(userPermissions);

                Result result = new Result
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
                    RolePermissions = rolePermissions.ToArray(),
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