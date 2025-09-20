using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;

using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
using UseCases.Common.Security.Authorization.Permissions;
using UseCases.Admin.Roles.Create;
using UseCases.Admin.Roles.GetList;
using UseCases.Admin.Roles.GetByName;
using UseCases.Admin.Roles.Update;
using UseCases.Admin.Roles.Delete;

namespace UseCases.Admin.Roles;

public sealed class RoleManagementEndpoint : ICarterModule
{
    internal const string Route = "api/admin/roles";
    internal const string Tag = "Role Management";
    internal const string Description = "Administrative endpoints for role management through Keycloak including CRUD operations";
    internal const string Summary = "Keycloak Role Management API";
    internal const string Name = "RoleManagement";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(Tag)
            .WithSummary(Summary)
            .WithDescription(Description)
            .RequireAuthorization();

        // Create role in Keycloak  
        group.MapPost("", async (
            [FromBody] CreateRoleRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new CreateRoleCommand.CreateRoleParam
            {
                Name = request.Name,
                Description = request.Description
            };

            var command = new CreateRoleCommand.Command(param);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var roleResult = result.Value;
            var apiResponse = ApiResponse<CreateRoleCommand.CreateRoleResult>.Created(roleResult, "Role created successfully in Keycloak");

            // Add HATEOAS links
            apiResponse
                .WithLink("self", $"{Route}/{request.Name}")
                .WithLink("update", $"{Route}/{request.Name}")
                .WithLink("delete", $"{Route}/{request.Name}")
                .WithLink("all-roles", Route)
                .WithLink("users", "/api/admin/users")
                .WithMetadata("source", "keycloak")
                .WithMetadata("roleType", "realm-role");

            return TypedResults.Ok(apiResponse);
        })
        .WithName("CreateKeycloakRole")
        .WithSummary(CreateRoleCommand.Summary)
        .WithDescription(CreateRoleCommand.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<CreateRoleCommand.CreateRoleResult>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.Create);

        // List roles from Keycloak
        group.MapGet("", async (
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRolesQuery.Query();
            var result = await mediator.Send(query, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var roleList = result.Value;
            var apiResponse = ApiResponse<List<GetRolesQuery.RoleResponse>>.Success(roleList, "Roles retrieved successfully from Keycloak");

            // Add management links
            apiResponse
                .WithLink("self", Route)
                .WithLink("create-role", Route)
                .WithLink("users", "/api/admin/users")
                .WithMetadata("source", "keycloak")
                .WithMetadata("totalRoles", roleList.Count)
                .WithMetadata("roleType", "realm-roles");

            return TypedResults.Ok(apiResponse);
        })
        .WithName("GetKeycloakRoles")  
        .WithSummary(GetRolesQuery.Summary)
        .WithDescription(GetRolesQuery.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetRolesQuery.RoleResponse>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.List);

        // Get role by name from Keycloak
        group.MapGet("/{roleName}", async (
            [FromRoute] string roleName,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRoleByNameQuery.Query(roleName);
            var result = await mediator.Send(query, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var roleDetail = result.Value;
            var apiResponse = ApiResponse<GetRoleByNameQuery.RoleDetailResponse>.Success(roleDetail, "Role details retrieved successfully from Keycloak");

            // Add role-specific management links
            apiResponse
                .WithLink("self", $"{Route}/{roleName}")
                .WithLink("update", $"{Route}/{roleName}")
                .WithLink("delete", $"{Route}/{roleName}")
                .WithLink("all-roles", Route)
                .WithLink("users", "/api/admin/users")
                .WithMetadata("source", "keycloak")
                .WithMetadata("roleName", roleName);

            return TypedResults.Ok(apiResponse);
        })
        .WithName("GetKeycloakRoleByName")
        .WithSummary(GetRoleByNameQuery.Summary)
        .WithDescription(GetRoleByNameQuery.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<GetRoleByNameQuery.RoleDetailResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.Read);

        // Update role in Keycloak
        group.MapPut("/{roleName}", async (
            [FromRoute] string roleName,
            [FromBody] UpdateRoleRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new UpdateRoleCommand.UpdateRoleParam
            {
                Name = request.Name,
                Description = request.Description
            };

            var command = new UpdateRoleCommand.Command(roleName, param);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var updateResult = result.Value;
            var apiResponse = ApiResponse<UpdateRoleCommand.UpdateRoleResult>.Success(updateResult, "Role updated successfully in Keycloak");

            // Add management links and update metadata  
            apiResponse
                .WithLink("self", $"{Route}/{updateResult.Name}")
                .WithLink("delete", $"{Route}/{updateResult.Name}")
                .WithLink("all-roles", Route)
                .WithLink("users", "/api/admin/users")
                .WithMetadata("source", "keycloak")
                .WithMetadata("updatedAt", DateTime.UtcNow)
                .WithMetadata("operation", "update");

            return TypedResults.Ok(apiResponse);
        })
        .WithName("UpdateKeycloakRole")
        .WithSummary(UpdateRoleCommand.Summary)
        .WithDescription(UpdateRoleCommand.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<UpdateRoleCommand.UpdateRoleResult>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.Update);

        // Delete role from Keycloak
        group.MapDelete("/{roleName}", async (
            [FromRoute] string roleName,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteRoleCommand.Command(roleName);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var deleteResult = result.Value;
            var apiResponse = ApiResponse<DeleteRoleCommand.DeleteRoleResult>.Success(deleteResult, "Role deleted successfully from Keycloak");

            // Add audit metadata and navigation links
            apiResponse
                .WithLink("all-roles", Route)
                .WithLink("create-role", Route)
                .WithLink("users", "/api/admin/users")
                .WithMetadata("source", "keycloak")
                .WithMetadata("deletedAt", DateTime.UtcNow)
                .WithMetadata("deletedRoleName", roleName)
                .WithMetadata("operation", "delete");

            return TypedResults.Ok(apiResponse);
        })
        .WithName("DeleteKeycloakRole")
        .WithSummary(DeleteRoleCommand.Summary)
        .WithDescription(DeleteRoleCommand.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<DeleteRoleCommand.DeleteRoleResult>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.Delete);
    }
}

// Request Models
public record CreateRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record UpdateRoleRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
}