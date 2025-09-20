using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Admin.Users.GetById;

public static partial class GetUserByIdQuery
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : IQueryHandler<Query, UserDetailResponse>
    {
        public async Task<ErrorOr<UserDetailResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await keycloakService.GetUserByIdAsync(request.Id, cancellationToken);
                if (user is null)
                {
                    return Error.NotFound("User.NotFound", $"User with ID {request.Id} not found");
                }

                var response = new UserDetailResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Enabled = user.Enabled,
                    EmailVerified = user.EmailVerified,
                    CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(user.CreatedTimestamp),
                    Attributes = user.Attributes
                };

                // Get user roles
                var roles = await keycloakService.GetUserRolesAsync(request.Id, cancellationToken);
                response.Roles = roles.Select(r => r.Name).ToList();

                return response;
            }
            catch (Exception ex)
            {
                return Error.Failure("GetUserById.Failed", $"Failed to retrieve user: {ex.Message}");
            }
        }
    }
}