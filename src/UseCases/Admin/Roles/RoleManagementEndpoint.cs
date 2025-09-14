using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models.Filter;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Paging;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

using UseCases.Admin.Permissions;
using UseCases.Admin.Roles.Create;
using UseCases.Admin.Roles.Delete;
using UseCases.Admin.Roles.GetById;
using UseCases.Admin.Roles.List;
using UseCases.Admin.Roles.Permissions.AssignBatch;
using UseCases.Admin.Roles.Update;
using UseCases.Admin.Roles.Users.AssignBatch;
using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
using UseCases.Common.Security.Authorization.Permissions;

namespace UseCases.Admin.Roles;

public sealed class RoleManagementEndpoint : ICarterModule
{
    internal const string Route = "api/admin/roles";
    internal const string Tag = "Role Management";
    internal const string Description = "Administrative endpoints for role management including CRUD operations, user assignment, and permission management";
    internal const string Summary = "Role Management API";
    internal const string Name = "RoleManagement";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(Tag)
            .WithSummary(Summary)
            .WithDescription(Description)
            .RequireAuthorization(); // All endpoints require authentication

        // Create role
        group.MapPost("", async (
            [FromBody] CreateRole.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateRole.Command(param);
            var result = await mediator.Send(command, cancellationToken);
            return result.ToTypedResult();
        })
        .WithName(CreateRole.Name)
        .WithSummary(CreateRole.Summary)
        .WithDescription(CreateRole.Description)
        .WithTags(Tag)
        .Produces<CreateRole.Result>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.Create);

        // List roles with pagination
        group.MapGet("", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromQuery] bool? IsSystemRole,
            [FromQuery] bool? IsDefault,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new ListRoles.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter,
                IsSystemRole = IsSystemRole,
                IsDefault = IsDefault
            };
            var query = new ListRoles.Query(param);
            var result = await mediator.Send(query, cancellationToken);
            return result.ToTypedResult();
        })
        .WithName(ListRoles.Name)
        .WithSummary(ListRoles.Summary)
        .WithDescription(ListRoles.Description)
        .WithTags(ListRoles.Tag)
        .Produces<PagedList<ListRoles.Result>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.List);

        // Get role by ID
        group.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new GetRoleById.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            var query = new GetRoleById.Query(id, param);
            var result = await mediator.Send(query, cancellationToken);
            return result.ToTypedResult();
        })
        .WithName(GetRoleById.Name)
        .WithSummary(GetRoleById.Summary)
        .WithDescription(GetRoleById.Description)
        .WithTags(Tag)
        .Produces<GetRoleById.Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.Read);

        // Update role
        group.MapPut("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateRole.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateRole.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);
            return result.ToTypedResult();
        })
        .WithName(UpdateRole.Name)
        .WithSummary(UpdateRole.Summary)
        .WithDescription(UpdateRole.Description)
        .WithTags(UpdateRole.Tag)
        .Produces<UpdateRole.Result>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.Update);

        // Delete role
        group.MapDelete("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteRole.Command(id);
            ErrorOr.ErrorOr<ErrorOr.Deleted> result = await mediator.Send(command, cancellationToken);
            return result.ToTypedResultDeleted();
        })
        .WithName(DeleteRole.Name)
        .WithSummary(DeleteRole.Summary)
        .WithDescription(DeleteRole.Description)
        .WithTags(Tag)
        .Produces<NoContent>(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.Delete);

        // Assign multiple users to role (batch)
        group.MapPost("/{id:guid}/users/batch", async (
            [FromRoute] Guid id,
            [FromBody] AssignRoleToBatchUsers.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignRoleToBatchUsers.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);
            return result.ToTypedResult();
        })
        .WithName(AssignRoleToBatchUsers.Name)
        .WithSummary(AssignRoleToBatchUsers.Summary)
        .WithDescription(AssignRoleToBatchUsers.Description)
        .WithTags(AssignRoleToBatchUsers.Tag, Tag)
        .Produces<AssignRoleToBatchUsers.Result>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.Update);

        // Assign multiple permissions to role (batch)
        group.MapPost("/{id:guid}/permissions/batch", async (
            [FromRoute] Guid id,
            [FromBody] AssignBatchPermissionsToRole.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignBatchPermissionsToRole.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);
            return result.ToTypedResult();
        })
        .WithName(AssignBatchPermissionsToRole.Name)
        .WithSummary(AssignBatchPermissionsToRole.Summary)
        .WithDescription(AssignBatchPermissionsToRole.Description)
        .WithTags(PermissionManagementEndpoint.Tag, AssignBatchPermissionsToRole.Tag, Tag)
        .Produces<AssignBatchPermissionsToRole.Result>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.AccessPermission.Assign);
    }

}