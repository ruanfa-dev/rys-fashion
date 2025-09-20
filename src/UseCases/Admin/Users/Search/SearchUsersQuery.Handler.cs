using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Admin.Users.Search;

public static partial class SearchUsersQuery
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : IQueryHandler<Query, SearchUsersResult>
    {
        public async Task<ErrorOr<SearchUsersResult>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;
                
                // Get all users first (in production, you'd want to implement server-side filtering)
                var allUsers = await keycloakService.GetUsersAsync(cancellationToken);
                
                // Apply client-side filtering
                var filteredUsers = allUsers.AsEnumerable();

                if (!string.IsNullOrEmpty(param.Username))
                {
                    filteredUsers = filteredUsers.Where(u => 
                        u.Username.Contains(param.Username, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(param.Email))
                {
                    filteredUsers = filteredUsers.Where(u => 
                        u.Email.Contains(param.Email, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(param.FirstName))
                {
                    filteredUsers = filteredUsers.Where(u => 
                        u.FirstName.Contains(param.FirstName, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(param.LastName))
                {
                    filteredUsers = filteredUsers.Where(u => 
                        u.LastName.Contains(param.LastName, StringComparison.OrdinalIgnoreCase));
                }

                if (param.Enabled.HasValue)
                {
                    filteredUsers = filteredUsers.Where(u => u.Enabled == param.Enabled.Value);
                }

                if (param.EmailVerified.HasValue)
                {
                    filteredUsers = filteredUsers.Where(u => u.EmailVerified == param.EmailVerified.Value);
                }

                var userList = filteredUsers.ToList();
                var totalCount = userList.Count;

                // Apply pagination
                if (param.First.HasValue)
                {
                    userList = userList.Skip(param.First.Value).ToList();
                }

                if (param.Max.HasValue)
                {
                    userList = userList.Take(param.Max.Value).ToList();
                }

                // Get roles for each user (this could be expensive - consider caching)
                var searchResults = new List<UserSearchItem>();
                
                foreach (var user in userList)
                {
                    var roles = await keycloakService.GetUserRolesAsync(user.Id, cancellationToken);
                    
                    searchResults.Add(new UserSearchItem
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Enabled = user.Enabled,
                        EmailVerified = user.EmailVerified,
                        CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(user.CreatedTimestamp),
                        Roles = roles.Select(r => r.Name).ToList()
                    });
                }

                return new SearchUsersResult
                {
                    Users = searchResults,
                    TotalCount = totalCount,
                    HasMore = param.First.HasValue && param.Max.HasValue && 
                              (param.First.Value + param.Max.Value) < totalCount
                };
            }
            catch (Exception ex)
            {
                return Error.Failure("SearchUsers.Failed", $"Failed to search users: {ex.Message}");
            }
        }
    }
}