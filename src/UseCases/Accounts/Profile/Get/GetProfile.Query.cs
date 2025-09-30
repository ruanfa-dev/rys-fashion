using Core.Identity.Users;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Profile.Common;
using UseCases.Common.Security.Authentication.Contexts;

namespace UseCases.Accounts.Profile.Get;
public static partial class GetProfile
{
    public sealed record Query : IQuery<AccountProfileResult>;
    public sealed class Handler(UserManager<User> dbContext, IUserContext userContext) : IQueryHandler<Query, AccountProfileResult>
    {
        public async Task<ErrorOr<AccountProfileResult>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Load: user context
            Guid? userId = userContext.UserId;
            bool isAuthenticated = userContext.IsAuthenticated;

            // Check: user is authenticated
            if (userId is null || !isAuthenticated)
                return User.Errors.UserUnauthorized;

            AccountProfileResult? user = await dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new AccountProfileResult
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    ProfileImagePath = u.ProfileImagePath,
                    Email = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber,
                    LastSignInAt = u.LastSignInAt,
                    LastSignInIp = u.LastSignInIp
                })
                .FirstOrDefaultAsync(cancellationToken);

            return user is null
                ? User.Errors.UserNotFound
                : user;
        }
    }
}
