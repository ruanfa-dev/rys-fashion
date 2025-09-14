using Core.Identity;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Authentication.Sessions;
using UseCases.Common.Security.Authentication.Contexts;
using UseCases.Common.Security.Authorization.Providers;

namespace UseCases.Accounts.Sessions.Get;
public static partial class GetSession
{
    public sealed record Query : IQuery<AccountSessionResult>;
    public sealed class Handler(
       IUserContext userContext,
       UserManager<User> userManager,
       IUserAuthorizationProvider userAuthorizationProvider) : IQueryHandler<Query, AccountSessionResult>
    {
        public async Task<ErrorOr<AccountSessionResult>> Handle(Query request, CancellationToken cancellationToken)
        {
            var userId = userContext.UserId;
            var isAuthenticated = userContext.IsAuthenticated;

            // Check: user is authenticated
            if (userId is null || !isAuthenticated)
                return User.Errors.UserUnauthorized;

            var user = await userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new AccountSessionResult
                {
                    UserId = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber,
                    IsEmailConfirmed = u.EmailConfirmed,
                    IsPhoneNumberConfirmed = u.PhoneNumberConfirmed,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null)
                return User.Errors.UserNotFound;

            var authData = await userAuthorizationProvider.GetUserAuthorizationAsync(userId.Value);
            if (authData is null)
                return User.Errors.UserUnauthorized;

            user.Roles = authData.Roles.ToList();
            user.Permissions = authData.Permissions.ToList();

            return user;
        }
    }
}
