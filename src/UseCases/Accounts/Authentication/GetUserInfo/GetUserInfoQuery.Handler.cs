using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Accounts.Authentication.GetUserInfo;

public static partial class GetUserInfoQuery
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : IQueryHandler<Query, UserInfoResult>
    {
        public async Task<ErrorOr<UserInfoResult>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var userInfo = await keycloakService.GetUserInfoAsync(request.AccessToken, cancellationToken);
                
                return new UserInfoResult
                {
                    Subject = userInfo.Subject,
                    Username = userInfo.PreferredUsername,
                    Email = userInfo.Email,
                    EmailVerified = userInfo.EmailVerified,
                    FirstName = userInfo.GivenName,
                    LastName = userInfo.FamilyName,
                    Roles = userInfo.RealmAccess?.Roles ?? new List<string>()
                };
            }
            catch (Exception ex)
            {
                return Error.Unauthorized("UserInfo.Failed", $"Failed to get user info: {ex.Message}");
            }
        }
    }
}