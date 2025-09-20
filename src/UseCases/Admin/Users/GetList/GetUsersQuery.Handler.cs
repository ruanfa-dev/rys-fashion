using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Admin.Users.GetList;

public static partial class GetUsersQuery
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : IQueryHandler<Query, List<UserResponse>>
    {
        public async Task<ErrorOr<List<UserResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;
                var users = await keycloakService.GetUsersAsync(cancellationToken);
                
                // Apply search filter if provided
                if (!string.IsNullOrEmpty(param.Search))
                {
                    users = users.Where(u => 
                        u.Username.Contains(param.Search, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(param.Search, StringComparison.OrdinalIgnoreCase) ||
                        u.FirstName.Contains(param.Search, StringComparison.OrdinalIgnoreCase) ||
                        u.LastName.Contains(param.Search, StringComparison.OrdinalIgnoreCase));
                }

                // Apply pagination
                if (param.First.HasValue)
                {
                    users = users.Skip(param.First.Value);
                }
                
                if (param.Max.HasValue)
                {
                    users = users.Take(param.Max.Value);
                }

                var userList = users.Select(u => new UserResponse
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Enabled = u.Enabled,
                    EmailVerified = u.EmailVerified,
                    CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(u.CreatedTimestamp)
                }).ToList();

                return userList;
            }
            catch (Exception ex)
            {
                return Error.Failure("GetUsers.Failed", $"Failed to retrieve users: {ex.Message}");
            }
        }
    }
}