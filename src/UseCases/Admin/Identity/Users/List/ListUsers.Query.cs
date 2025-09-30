using Core.Identity.Users;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;
using SharedKernel.Models.PagedLists;

using UseCases.Admin.Identity.Users.Common;

namespace UseCases.Admin.Identity.Users.List;

public static partial class ListUsers
{
    internal const string Route = "";
    internal const string Tag = "User Management";
    internal const string Name = "ListUsers";
    internal const string Summary = "List users with pagination";
    internal const string Description = "Retrieves a paginated list of users with their basic information and roles";

    public sealed record Query(
        int Page = 1,
        int PageSize = 20,
        string? SearchTerm = null,
        string? Role = null,
        bool? IsActive = null,
        bool? EmailConfirmed = null
    ) : IQuery<PagedList<Result>>;

    public sealed record Result : UserResult.ListItem;

    public sealed class Handler(
        UserManager<User> userManager,
        ILogger<Handler> logger
    ) : IQueryHandler<Query, PagedList<Result>>
    {
        public async Task<ErrorOr<PagedList<Result>>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<User> query = userManager.Users.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    string searchTerm = request.SearchTerm.ToLower();
                    query = query.Where(u => 
                        u.Email!.ToLower().Contains(searchTerm) ||
                        u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm) ||
                        u.LastName != null && u.LastName.ToLower().Contains(searchTerm) ||
                        u.UserName != null && u.UserName.ToLower().Contains(searchTerm)
                    );
                }

                if (request.EmailConfirmed.HasValue)
                {
                    query = query.Where(u => u.EmailConfirmed == request.EmailConfirmed.Value);
                }

                // Get total count before pagination
                int totalCount = await query.CountAsync(cancellationToken);

                // Apply pagination
                List<User> users = await query
                    .OrderBy(u => u.Email)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                // Get roles for each user
                List<Result> results = new List<Result>();
                foreach (User user in users)
                {
                    IList<string> roles = await userManager.GetRolesAsync(user);
                    
                    // Apply role filter if specified
                    if (!string.IsNullOrWhiteSpace(request.Role) && !roles.Contains(request.Role))
                    {
                        continue;
                    }

                    results.Add(new Result
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
                        Roles = roles.ToArray(),
                        LastSignInAt = user.LastSignInAt,
                        SignInCount = user.SignInCount
                    });
                }

                // If role filter was applied, we need to recalculate total count
                if (!string.IsNullOrWhiteSpace(request.Role))
                {
                    totalCount = results.Count;
                }

                PagedList<Result> pagedResult = new PagedList<Result>(
                    results,
                    request.Page,
                    request.PageSize,
                    totalCount
                );

                logger.LogDebug("Retrieved {Count} users for page {Page}", results.Count, request.Page);

                return pagedResult;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving users list");
                return Error.Failure("Users.RetrievalFailed", "Failed to retrieve users list");
            }
        }
    }
}