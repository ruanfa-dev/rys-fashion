using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;

using UseCases.Admin.Permissions;
using UseCases.Admin.Users.Create;
using UseCases.Admin.Users.Delete;
using UseCases.Admin.Users.GetById;
using UseCases.Admin.Users.List;
using UseCases.Admin.Users.Permissions.AssignBatch;
using UseCases.Admin.Users.Roles.AssignBatch;
using UseCases.Admin.Users.Update;
using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
using UseCases.Common.Security.Authorization.Permissions;

namespace UseCases.Admin.Users;

public sealed class UserManagementEndpoint : ICarterModule
{
    internal const string Route = "api/admin/users";
    internal const string Tag = "User Management";
    internal const string Description = "Administrative endpoints for user management including CRUD operations, role assignment, and permissions";
    internal const string Summary = "User Management API";
    internal const string Name = "UserManagement";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(Tag)
            .WithSummary(Summary)
            .WithDescription(Description)
            .RequireAuthorization();

        // Create user
        group.MapPost("", async (
            [FromBody] CreateUser.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateUser.Command(param);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponseCreated("User created successfully");

            // Add admin user management HATEOAS links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("update", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("delete", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("assign-roles", $"{Route}/{apiResponse.Data.Id}/roles/batch")
                    .WithLink("assign-permissions", $"{Route}/{apiResponse.Data.Id}/permissions/batch")
                    .WithLink("all-users", Route)
                    .WithMetadata("adminAction", "user-creation")
                    .WithMetadata("userType", "admin-managed");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(CreateUser.Name)
        .WithSummary(CreateUser.Summary)
        .WithDescription(CreateUser.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<CreateUser.Result>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.User.Create);

        // List users with pagination
        group.MapGet("", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] string? searchTerm,
            [FromQuery] string? role,
            [FromQuery] bool? isActive,
            [FromQuery] bool? emailConfirmed,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new ListUsers.Query(page, pageSize, searchTerm, role, isActive, emailConfirmed);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponsePaged("Users retrieved successfully");

            // Add pagination and admin management links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                // Add pagination links
                apiResponse.WithLink("self", $"{Route}?page_index={page}&page_size={pageSize}");

                if (apiResponse.Pagination?.HasPrevious == true)
                {
                    apiResponse.WithLink("prev", $"{Route}?page_index={page - 1}&page_size={pageSize}");
                }

                if (apiResponse.Pagination?.HasNext == true)
                {
                    apiResponse.WithLink("next", $"{Route}?page_index={page + 1}&page_size={pageSize}");
                }

                apiResponse.WithLink("first", $"{Route}?page_index=1&page_size={pageSize}");

                if (apiResponse.Pagination?.TotalPages > 0)
                {
                    apiResponse.WithLink("last", $"{Route}?page_index={apiResponse.Pagination.TotalPages}&page_size={pageSize}");
                }

                // Add admin management links
                apiResponse
                    .WithLink("create-user", Route)
                    .WithLink("roles", "/api/admin/roles")
                    .WithLink("permissions", "/api/admin/permissions")
                    .WithMetadata("adminContext", "user-listing")
                    .WithMetadata("filterApplied", searchTerm != null || role != null || isActive != null || emailConfirmed != null);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(ListUsers.Name)
        .WithSummary(ListUsers.Summary)
        .WithDescription(ListUsers.Description)
        .WithTags(ListUsers.Tag)
        .Produces<ApiResponse<List<ListUsers.Result>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.User.List);

        // Get user by ID
        group.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserById.Query(id);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponse("User details retrieved successfully");

            // Add user-specific admin management links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("update", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("assign-roles", $"{Route}/{id}/roles/batch")
                    .WithLink("assign-permissions", $"{Route}/{id}/permissions/batch")
                    .WithLink("all-users", Route)
                    .WithMetadata("adminContext", "user-details")
                    .WithMetadata("userId", id);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetUserById.Name)
        .WithSummary(GetUserById.Summary)
        .WithDescription(GetUserById.Description)
        .WithTags(GetUserById.Tag)
        .Produces<ApiResponse<GetUserById.Result>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.User.View);

        // Update user
        group.MapPut("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateUser.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateUser.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponse("User updated successfully");

            // Add user management links and update metadata
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("assign-roles", $"{Route}/{id}/roles/batch")
                    .WithLink("assign-permissions", $"{Route}/{id}/permissions/batch")
                    .WithLink("all-users", Route)
                    .WithMetadata("adminAction", "user-update")
                    .WithMetadata("updatedAt", DateTime.UtcNow)
                    .WithMetadata("operation", "update");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(UpdateUser.Name)
        .WithSummary(UpdateUser.Summary)
        .WithDescription(UpdateUser.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<UpdateUser.Result>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.User.Update);

        // Delete user
        group.MapDelete("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteUser.Command(id);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponse("User deleted successfully");

            // Add admin audit metadata and navigation links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("all-users", Route)
                    .WithLink("create-user", Route)
                    .WithMetadata("adminAction", "user-deletion")
                    .WithMetadata("deletedAt", DateTime.UtcNow)
                    .WithMetadata("deletedUserId", id)
                    .WithMetadata("operation", "delete");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(DeleteUser.Name)
        .WithSummary(DeleteUser.Summary)
        .WithDescription(DeleteUser.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<DeleteUser.Result>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.User.Delete);

        // Assign multiple roles to user (batch)
        group.MapPost("/{id:guid}/roles/batch", async (
            [FromRoute] Guid id,
            [FromBody] AssignBatchRolesToUser.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignBatchRolesToUser.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponse("Roles assigned successfully");

            // Add role assignment metadata and links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("user-details", $"{Route}/{id}")
                    .WithLink("assign-permissions", $"{Route}/{id}/permissions/batch")
                    .WithLink("all-roles", "/api/admin/roles")
                    .WithLink("all-users", Route)
                    .WithMetadata("adminAction", "role-assignment")
                    .WithMetadata("assignedAt", DateTime.UtcNow)
                    .WithMetadata("targetUserId", id)
                    .WithMetadata("operation", "batch-role-assign");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(AssignBatchRolesToUser.Name)
        .WithSummary(AssignBatchRolesToUser.Summary)
        .WithDescription(AssignBatchRolesToUser.Description)
        .WithTags(AssignBatchRolesToUser.Tag, Tag)
        .Produces<ApiResponse<AssignBatchRolesToUser.Result>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.Assign);

        // Assign multiple permission to user (batch)
        group.MapPost("/{id:guid}/permissions/batch", async (
            [FromRoute] Guid id,
            [FromBody] AssignBatchPermissionsToUser.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignBatchPermissionsToUser.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponse("Permissions assigned successfully");

            // Add permission assignment metadata and links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("user-details", $"{Route}/{id}")
                    .WithLink("assign-roles", $"{Route}/{id}/roles/batch")
                    .WithLink("all-permissions", "/api/admin/permissions")
                    .WithLink("all-users", Route)
                    .WithMetadata("adminAction", "permission-assignment")
                    .WithMetadata("assignedAt", DateTime.UtcNow)
                    .WithMetadata("targetUserId", id)
                    .WithMetadata("operation", "batch-permission-assign");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(AssignBatchPermissionsToUser.Name)
        .WithSummary(AssignBatchPermissionsToUser.Summary)
        .WithDescription(AssignBatchPermissionsToUser.Description)
        .WithTags(PermissionManagementEndpoint.Tag, AssignBatchPermissionsToUser.Tag, Tag)
        .Produces<ApiResponse<AssignBatchPermissionsToUser.Result>>(StatusCodes.Status200OK)
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