using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;
using SharedKernel.Models.Filter;
using SharedKernel.Models.Paging;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

using UseCases.Admin.Permissions.List;
using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
using UseCases.Common.Security.Authorization.Permissions;

namespace UseCases.Admin.Identity.Permissions;

public sealed class PermissionManagementEndpoint : ICarterModule
{
    internal const string Route = "api/admin/permissions";
    internal const string Tag = "Permission Management";
    internal const string Description = "Administrative endpoints for permission management and discovery";
    internal const string Summary = "Permission Management API";
    internal const string Name = "PermissionManagement";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(Tag)
            .WithSummary(Summary)
            .WithDescription(Description)
            .RequireAuthorization(); // All endpoints require authentication

        // List all available permissions
        group.MapGet("/available", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new ListAvailablePermissions.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            var query = new ListAvailablePermissions.Query(param);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponsePaged("Available permissions retrieved successfully");

            // Add pagination and admin management links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                // Add pagination links
                var currentPage = (pagination.PageIndex ?? 0) + 1;
                var pageSize = pagination.PageSize ?? 10;

                apiResponse.WithLink("self", $"{Route}/available?page_index={currentPage}&page_size={pageSize}");

                if (apiResponse.Pagination?.HasPrevious == true)
                {
                    apiResponse.WithLink("prev", $"{Route}/available?page_index={currentPage - 1}&page_size={pageSize}");
                }

                if (apiResponse.Pagination?.HasNext == true)
                {
                    apiResponse.WithLink("next", $"{Route}/available?page_index={currentPage + 1}&page_size={pageSize}");
                }

                apiResponse.WithLink("first", $"{Route}/available?page_index=1&page_size={pageSize}");

                if (apiResponse.Pagination?.TotalPages > 0)
                {
                    apiResponse.WithLink("last", $"{Route}/available?page_index={apiResponse.Pagination.TotalPages}&page_size={pageSize}");
                }

                // Add admin management links for cross-navigation
                apiResponse
                    .WithLink("all-users", "/api/admin/users")
                    .WithLink("all-roles", "/api/admin/roles")
                    .WithMetadata("adminContext", "permission-discovery")
                    .WithMetadata("permissionType", "available")
                    .WithMetadata("filterApplied", !string.IsNullOrEmpty(search.SearchTerm) || filter.Filters?.Any() == true);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(ListAvailablePermissions.Name)
        .WithSummary(ListAvailablePermissions.Summary)
        .WithDescription(ListAvailablePermissions.Description)
        .WithTags(ListAvailablePermissions.Tag)
        .Produces<ApiResponse<List<ListAvailablePermissions.Result>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.AccessPermission.List);
    }
}