using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models.Filter;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Paging;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

using UseCases.Admin.Permissions.List;
using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
using UseCases.Common.Security.Authorization.Permissions;

namespace UseCases.Admin.Permissions;

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
            return result.ToTypedResult();
        })
        .WithName(ListAvailablePermissions.Name)
        .WithSummary(ListAvailablePermissions.Summary)
        .WithDescription(ListAvailablePermissions.Description)
        .WithTags(ListAvailablePermissions.Tag)
        .Produces<PagedList<ListAvailablePermissions.Result>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.AccessPermission.List);
    }
}