using Core.Identity;

using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;
using SharedKernel.Models.Filter;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Queries;
using SharedKernel.Models.Search;

using UseCases.Admin.Users.Common;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Users.List;

public static partial class ListUsers
{
    internal const string Route = "";
    internal const string Tag = "User Management";
    internal const string Name = "ListUsers";
    internal const string Summary = "List users with pagination";
    internal const string Description = "Retrieves a paginated list of users with their basic information and roles";

    public record Param : QueryParams
    {
        public bool? EmailConfirmed { get; init; }
        public string? Role { get; init; }
    }

    public sealed record Result : UserListResult;
    public sealed record Query(Param Param) : IQuery<PagedList<Result>>;

    public sealed class Handler(
        IApplicationDbContext context,
        ILogger<Handler> logger
    ) : IQueryHandler<Query, PagedList<Result>>
    {
        public async Task<ErrorOr<PagedList<Result>>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;

                var query = context.Set<User>()
                    .AsQueryable()
                    .AsNoTracking()
                    .Where(u => !param.EmailConfirmed.HasValue || u.EmailConfirmed == param.EmailConfirmed.Value)
                    .ApplySearch(param.Search)
                    .ApplyFilters(param.Filter)
                    // Apply role filter if specified (checks related Role name via UserRoles)
                    .Where(u => string.IsNullOrWhiteSpace(param.Role) || u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == param.Role));

                var projectedQuery = query
                    .OrderBy(u => u.Email)
                    .Select(u => new Result
                    {
                        Id = u.Id,
                        Email = u.Email!,
                        UserName = u.UserName,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        PhoneNumber = u.PhoneNumber,
                        ProfileImagePath = u.ProfileImagePath,
                        EmailConfirmed = u.EmailConfirmed,
                        CreatedAt = u.CreatedAt,
                        CreatedBy = u.CreatedBy,
                        Roles = u.UserRoles.Select(ur => ur.Role!.Name!).ToArray(),
                        LastSignInAt = u.LastSignInAt,
                        SignInCount = u.SignInCount
                    });

                var paginatedList = await projectedQuery
                    .ToPagedListAsync(
                        param.Paging,
                        cancellationToken: cancellationToken);

                logger.LogDebug("Retrieved {Count} users for page {Page}", paginatedList.Items.Count, param.Paging.PageSize);

                return paginatedList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving users list");
                return User.Errors.UserIdInvalidFormat
            }
        }
    }
}