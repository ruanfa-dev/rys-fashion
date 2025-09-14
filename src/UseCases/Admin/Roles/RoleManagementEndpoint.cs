using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;
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
            var apiResponse = result.ToApiResponseCreated("Role created successfully");
            
            // Add admin role management HATEOAS links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("update", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("delete", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("assign-users", $"{Route}/{apiResponse.Data.Id}/users/batch")
                    .WithLink("assign-permissions", $"{Route}/{apiResponse.Data.Id}/permissions/batch")
                    .WithLink("all-roles", Route)
                    .WithLink("all-users", "/api/admin/users")
                    .WithMetadata("adminAction", "role-creation")
                    .WithMetadata("roleType", param.IsSystemRole ? "system" : "custom");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(CreateRole.Name)
        .WithSummary(CreateRole.Summary)
        .WithDescription(CreateRole.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<CreateRole.Result>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
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
            var apiResponse = result.ToApiResponsePaged("Roles retrieved successfully");
            
            // Add pagination and admin management links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                // Add pagination links
                var currentPage = (pagination.PageIndex ?? 0) + 1;
                var pageSize = pagination.PageSize ?? 10;
                
                apiResponse.WithLink("self", $"{Route}?page_index={currentPage}&page_size={pageSize}");
                
                if (apiResponse.Pagination?.HasPrevious == true)
                {
                    apiResponse.WithLink("prev", $"{Route}?page_index={currentPage - 1}&page_size={pageSize}");
                }
                
                if (apiResponse.Pagination?.HasNext == true)
                {
                    apiResponse.WithLink("next", $"{Route}?page_index={currentPage + 1}&page_size={pageSize}");
                }
                
                apiResponse.WithLink("first", $"{Route}?page_index=1&page_size={pageSize}");
                
                if (apiResponse.Pagination?.TotalPages > 0)
                {
                    apiResponse.WithLink("last", $"{Route}?page_index={apiResponse.Pagination.TotalPages}&page_size={pageSize}");
                }
                
                // Add admin management links
                apiResponse
                    .WithLink("create-role", Route)
                    .WithLink("all-users", "/api/admin/users")
                    .WithLink("all-permissions", "/api/admin/permissions")
                    .WithMetadata("adminContext", "role-listing")
                    .WithMetadata("filterApplied", IsSystemRole != null || IsDefault != null || !string.IsNullOrEmpty(search.SearchTerm));
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(ListRoles.Name)
        .WithSummary(ListRoles.Summary)
        .WithDescription(ListRoles.Description)
        .WithTags(ListRoles.Tag)
        .Produces<ApiResponse<List<ListRoles.Result>>>(StatusCodes.Status200OK)
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
            var apiResponse = result.ToApiResponse("Role details retrieved successfully");
            
            // Add role-specific admin management links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("update", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("assign-users", $"{Route}/{id}/users/batch")
                    .WithLink("assign-permissions", $"{Route}/{id}/permissions/batch")
                    .WithLink("all-roles", Route)
                    .WithLink("all-users", "/api/admin/users")
                    .WithMetadata("adminContext", "role-details")
                    .WithMetadata("roleId", id);
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetRoleById.Name)
        .WithSummary(GetRoleById.Summary)
        .WithDescription(GetRoleById.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<GetRoleById.Result>>(StatusCodes.Status200OK)
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
            var apiResponse = result.ToApiResponse("Role updated successfully");
            
            // Add role management links and update metadata
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("assign-users", $"{Route}/{id}/users/batch")
                    .WithLink("assign-permissions", $"{Route}/{id}/permissions/batch")
                    .WithLink("all-roles", Route)
                    .WithMetadata("adminAction", "role-update")
                    .WithMetadata("updatedAt", DateTime.UtcNow)
                    .WithMetadata("operation", "update");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(UpdateRole.Name)
        .WithSummary(UpdateRole.Summary)
        .WithDescription(UpdateRole.Description)
        .WithTags(UpdateRole.Tag)
        .Produces<ApiResponse<UpdateRole.Result>>(StatusCodes.Status200OK)
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
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponseDeleted("Role deleted successfully");
            
            // Add admin audit metadata and navigation links
            apiResponse
                .WithLink("all-roles", Route)
                .WithLink("create-role", Route)
                .WithLink("all-users", "/api/admin/users")
                .WithMetadata("adminAction", "role-deletion")
                .WithMetadata("deletedAt", DateTime.UtcNow)
                .WithMetadata("deletedRoleId", id)
                .WithMetadata("operation", "delete");
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(DeleteRole.Name)
        .WithSummary(DeleteRole.Summary)
        .WithDescription(DeleteRole.Description)
        .WithTags(Tag)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
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
            var apiResponse = result.ToApiResponse("Users assigned to role successfully");
            
            // Add user assignment metadata and links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("role-details", $"{Route}/{id}")
                    .WithLink("assign-permissions", $"{Route}/{id}/permissions/batch")
                    .WithLink("all-users", "/api/admin/users")
                    .WithLink("all-roles", Route)
                    .WithMetadata("adminAction", "user-assignment")
                    .WithMetadata("assignedAt", DateTime.UtcNow)
                    .WithMetadata("targetRoleId", id)
                    .WithMetadata("operation", "batch-user-assign");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(AssignRoleToBatchUsers.Name)
        .WithSummary(AssignRoleToBatchUsers.Summary)
        .WithDescription(AssignRoleToBatchUsers.Description)
        .WithTags(AssignRoleToBatchUsers.Tag, Tag)
        .Produces<ApiResponse<AssignRoleToBatchUsers.Result>>(StatusCodes.Status200OK)
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
            var apiResponse = result.ToApiResponse("Permissions assigned to role successfully");
            
            // Add permission assignment metadata and links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("role-details", $"{Route}/{id}")
                    .WithLink("assign-users", $"{Route}/{id}/users/batch")
                    .WithLink("all-permissions", "/api/admin/permissions")
                    .WithLink("all-roles", Route)
                    .WithMetadata("adminAction", "permission-assignment")
                    .WithMetadata("assignedAt", DateTime.UtcNow)
                    .WithMetadata("targetRoleId", id)
                    .WithMetadata("operation", "batch-permission-assign");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(AssignBatchPermissionsToRole.Name)
        .WithSummary(AssignBatchPermissionsToRole.Summary)
        .WithDescription(AssignBatchPermissionsToRole.Description)
        .WithTags(PermissionManagementEndpoint.Tag, AssignBatchPermissionsToRole.Tag, Tag)
        .Produces<ApiResponse<AssignBatchPermissionsToRole.Result>>(StatusCodes.Status200OK)
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