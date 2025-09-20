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
using UseCases.Admin.Users.Create;
using UseCases.Admin.Users.GetList;
using UseCases.Admin.Users.GetById;
using UseCases.Admin.Users.Update;
using UseCases.Admin.Users.Delete;
using UseCases.Admin.Users.AssignRoles;
using UseCases.Admin.Users.RemoveRoles;
using UseCases.Admin.Users.ResetPassword;

namespace UseCases.Admin.Users;

public sealed class UserManagementEndpoint : ICarterModule
{
    internal const string Route = "api/admin/users";
    internal const string Tag = "User Management";
    internal const string Description = "Administrative endpoints for user management through Keycloak including CRUD operations, role assignment, and permissions";
    internal const string Summary = "Keycloak User Management API";
    internal const string Name = "UserManagement";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(Tag)
            .WithSummary(Summary)
            .WithDescription(Description)
            .RequireAuthorization();

        // Create user in Keycloak
        group.MapPost("", async (
            [FromBody] CreateUserRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new CreateUserCommand.CreateUserParam
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Enabled = request.Enabled,
                EmailVerified = request.EmailVerified,
                Password = request.Password,
                TemporaryPassword = request.TemporaryPassword,
                RoleNames = request.RoleNames,
                Attributes = request.Attributes
            };

            var command = new CreateUserCommand.Command(param);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var userResult = result.Value;
            var apiResponse = ApiResponse<CreateUserCommand.CreateUserResult>.Created(userResult, "User created successfully in Keycloak");

            // Add HATEOAS links
            apiResponse
                .WithLink("self", $"{Route}/{userResult.Id}")
                .WithLink("update", $"{Route}/{userResult.Id}")
                .WithLink("delete", $"{Route}/{userResult.Id}")
                .WithLink("assign-roles", $"{Route}/{userResult.Id}/roles")
                .WithLink("all-users", Route)
                .WithMetadata("source", "keycloak")
                .WithMetadata("userType", "keycloak-managed");

            return TypedResults.Ok(apiResponse);
        })
        .WithName("CreateKeycloakUser")
        .WithSummary(CreateUserCommand.Summary)
        .WithDescription(CreateUserCommand.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<CreateUserCommand.CreateUserResult>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.User.Create);

        // List users from Keycloak with search
        group.MapGet("", async (
            [FromQuery] string? search,
            [FromQuery] int? max,
            [FromQuery] int? first,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new GetUsersQuery.GetUsersParam
            {
                Search = search,
                Max = max,
                First = first
            };

            var query = new GetUsersQuery.Query(param);
            var result = await mediator.Send(query, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var userList = result.Value;
            var apiResponse = ApiResponse<List<GetUsersQuery.UserResponse>>.Success(userList, "Users retrieved successfully from Keycloak");

            // Add pagination and management links
            apiResponse
                .WithLink("self", $"{Route}?search={search}&max={max}&first={first}")
                .WithLink("create-user", Route)
                .WithLink("roles", "/api/admin/roles")
                .WithMetadata("source", "keycloak")
                .WithMetadata("totalReturned", userList.Count)
                .WithMetadata("searchApplied", !string.IsNullOrEmpty(search));

            return TypedResults.Ok(apiResponse);
        })
        .WithName("GetKeycloakUsers")
        .WithSummary(GetUsersQuery.Summary)
        .WithDescription(GetUsersQuery.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetUsersQuery.UserResponse>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.User.List);

        // Get user by ID from Keycloak
        group.MapGet("/{id}", async (
            [FromRoute] string id,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserByIdQuery.Query(id);
            var result = await mediator.Send(query, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var userDetail = result.Value;
            var apiResponse = ApiResponse<GetUserByIdQuery.UserDetailResponse>.Success(userDetail, "User details retrieved successfully from Keycloak");

            // Add user-specific management links
            apiResponse
                .WithLink("self", $"{Route}/{id}")
                .WithLink("update", $"{Route}/{id}")
                .WithLink("delete", $"{Route}/{id}")
                .WithLink("assign-roles", $"{Route}/{id}/roles")
                .WithLink("reset-password", $"{Route}/{id}/password")
                .WithLink("all-users", Route)
                .WithMetadata("source", "keycloak")
                .WithMetadata("userId", id);

            return TypedResults.Ok(apiResponse);
        })
        .WithName("GetKeycloakUserById")
        .WithSummary(GetUserByIdQuery.Summary)
        .WithDescription(GetUserByIdQuery.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<GetUserByIdQuery.UserDetailResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.User.View);

        // Update user in Keycloak
        group.MapPut("/{id}", async (
            [FromRoute] string id,
            [FromBody] UpdateUserRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new UpdateUserCommand.UpdateUserParam
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Enabled = request.Enabled,
                EmailVerified = request.EmailVerified,
                Attributes = request.Attributes
            };

            var command = new UpdateUserCommand.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var updateResult = result.Value;
            var apiResponse = ApiResponse<UpdateUserCommand.UpdateUserResult>.Success(updateResult, "User updated successfully in Keycloak");

            // Add management links and update metadata
            apiResponse
                .WithLink("self", $"{Route}/{id}")
                .WithLink("delete", $"{Route}/{id}")
                .WithLink("assign-roles", $"{Route}/{id}/roles")
                .WithLink("all-users", Route)
                .WithMetadata("source", "keycloak")
                .WithMetadata("updatedAt", DateTime.UtcNow)
                .WithMetadata("operation", "update");

            return TypedResults.Ok(apiResponse);
        })
        .WithName("UpdateKeycloakUser")
        .WithSummary(UpdateUserCommand.Summary)
        .WithDescription(UpdateUserCommand.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<UpdateUserCommand.UpdateUserResult>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.User.Update);

        // Delete user from Keycloak
        group.MapDelete("/{id}", async (
            [FromRoute] string id,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteUserCommand.Command(id);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var deleteResult = result.Value;
            var apiResponse = ApiResponse<DeleteUserCommand.DeleteUserResult>.Success(deleteResult, "User deleted successfully from Keycloak");

            // Add audit metadata and navigation links
            apiResponse
                .WithLink("all-users", Route)
                .WithLink("create-user", Route)
                .WithMetadata("source", "keycloak")
                .WithMetadata("deletedAt", DateTime.UtcNow)
                .WithMetadata("deletedUserId", id)
                .WithMetadata("operation", "delete");

            return TypedResults.Ok(apiResponse);
        })
        .WithName("DeleteKeycloakUser")
        .WithSummary(DeleteUserCommand.Summary)
        .WithDescription(DeleteUserCommand.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<DeleteUserCommand.DeleteUserResult>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.User.Delete);

        // Assign roles to user in Keycloak
        group.MapPost("/{id}/roles", async (
            [FromRoute] string id,
            [FromBody] AssignRolesRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new AssignRolesToUserCommand.AssignRolesParam
            {
                RoleNames = request.RoleNames
            };

            var command = new AssignRolesToUserCommand.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var assignResult = result.Value;
            var apiResponse = ApiResponse<AssignRolesToUserCommand.AssignRolesResult>.Success(assignResult, "Roles assigned successfully in Keycloak");

            // Add role assignment metadata and links
            apiResponse
                .WithLink("user-details", $"{Route}/{id}")
                .WithLink("remove-roles", $"{Route}/{id}/roles")
                .WithLink("all-roles", "/api/admin/roles")
                .WithLink("all-users", Route)
                .WithMetadata("source", "keycloak")
                .WithMetadata("assignedAt", DateTime.UtcNow)
                .WithMetadata("targetUserId", id)
                .WithMetadata("operation", "role-assign");

            return TypedResults.Ok(apiResponse);
        })
        .WithName("AssignKeycloakRolesToUser")
        .WithSummary(AssignRolesToUserCommand.Summary)
        .WithDescription(AssignRolesToUserCommand.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<AssignRolesToUserCommand.AssignRolesResult>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.Assign);

        // Remove roles from user in Keycloak
        group.MapDelete("/{id}/roles", async (
            [FromRoute] string id,
            [FromBody] RemoveRolesRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new RemoveRolesFromUserCommand.RemoveRolesParam
            {
                RoleNames = request.RoleNames
            };

            var command = new RemoveRolesFromUserCommand.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var removeResult = result.Value;
            var apiResponse = ApiResponse<RemoveRolesFromUserCommand.RemoveRolesResult>.Success(removeResult, "Roles removed successfully in Keycloak");

            // Add role removal metadata and links
            apiResponse
                .WithLink("user-details", $"{Route}/{id}")
                .WithLink("assign-roles", $"{Route}/{id}/roles")
                .WithLink("all-roles", "/api/admin/roles")
                .WithLink("all-users", Route)
                .WithMetadata("source", "keycloak")
                .WithMetadata("removedAt", DateTime.UtcNow)
                .WithMetadata("targetUserId", id)
                .WithMetadata("operation", "role-remove");

            return TypedResults.Ok(apiResponse);
        })
        .WithName("RemoveKeycloakRolesFromUser")
        .WithSummary(RemoveRolesFromUserCommand.Summary)
        .WithDescription(RemoveRolesFromUserCommand.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<RemoveRolesFromUserCommand.RemoveRolesResult>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Role.Assign);

        // Reset user password in Keycloak
        group.MapPost("/{id}/password", async (
            [FromRoute] string id,
            [FromBody] ResetPasswordRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new ResetUserPasswordCommand.ResetPasswordParam
            {
                Password = request.Password,
                Temporary = request.Temporary
            };

            var command = new ResetUserPasswordCommand.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var resetResult = result.Value;
            var apiResponse = ApiResponse<ResetUserPasswordCommand.ResetPasswordResult>.Success(resetResult, "Password reset successfully in Keycloak");

            // Add password reset metadata and links
            apiResponse
                .WithLink("user-details", $"{Route}/{id}")
                .WithLink("all-users", Route)
                .WithMetadata("source", "keycloak")
                .WithMetadata("resetAt", DateTime.UtcNow)
                .WithMetadata("targetUserId", id)
                .WithMetadata("temporary", request.Temporary)
                .WithMetadata("operation", "password-reset");

            return TypedResults.Ok(apiResponse);
        })
        .WithName("ResetKeycloakUserPassword")
        .WithSummary(ResetUserPasswordCommand.Summary)
        .WithDescription(ResetUserPasswordCommand.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<ResetUserPasswordCommand.ResetPasswordResult>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.User.Update);
    }
}

// Request Models
public record CreateUserRequest
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public bool EmailVerified { get; init; } = false;
    public string? Password { get; init; }
    public bool TemporaryPassword { get; init; } = false;
    public IEnumerable<string>? RoleNames { get; init; }
    public Dictionary<string, object[]>? Attributes { get; init; }
}

public record UpdateUserRequest
{
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool? Enabled { get; init; }
    public bool? EmailVerified { get; init; }
    public Dictionary<string, object[]>? Attributes { get; init; }
}

public record AssignRolesRequest
{
    public IEnumerable<string> RoleNames { get; init; } = Array.Empty<string>();
}

public record RemoveRolesRequest
{
    public IEnumerable<string> RoleNames { get; init; } = Array.Empty<string>();
}

public record ResetPasswordRequest
{
    public string Password { get; init; } = string.Empty;
    public bool Temporary { get; init; } = false;
}