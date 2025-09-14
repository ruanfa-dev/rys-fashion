using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models.PagedLists;

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
            return result.ToTypedResult();
        })
        .WithName(CreateUser.Name)
        .WithSummary(CreateUser.Summary)
        .WithDescription(CreateUser.Description)
        .WithTags(Tag)
        .Produces<CreateUser.Result>(StatusCodes.Status201Created)
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
            return result.ToTypedResult();
        })
        .WithName(ListUsers.Name)
        .WithSummary(ListUsers.Summary)
        .WithDescription(ListUsers.Description)
        .WithTags(ListUsers.Tag)
        .Produces<PagedList<ListUsers.Result>>(StatusCodes.Status200OK)
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
            return result.ToTypedResult();
        })
        .WithName(GetUserById.Name)
        .WithSummary(GetUserById.Summary)
        .WithDescription(GetUserById.Description)
        .WithTags(GetUserById.Tag)
        .Produces<GetUserById.Result>(StatusCodes.Status200OK)
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
            return result.ToTypedResult();
        })
        .WithName(UpdateUser.Name)
        .WithSummary(UpdateUser.Summary)
        .WithDescription(UpdateUser.Description)
        .WithTags(Tag)
        .Produces<UpdateUser.Result>(StatusCodes.Status200OK)
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
            return result.ToTypedResult();
        })
        .WithName(DeleteUser.Name)
        .WithSummary(DeleteUser.Summary)
        .WithDescription(DeleteUser.Description)
        .WithTags(Tag)
        .Produces<DeleteUser.Result>(StatusCodes.Status200OK)
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
            return result.ToTypedResult();
        })
        .WithName(AssignBatchRolesToUser.Name)
        .WithSummary(AssignBatchRolesToUser.Summary)
        .WithDescription(AssignBatchRolesToUser.Description)
        .WithTags(AssignBatchRolesToUser.Tag, Tag)
        .Produces<AssignBatchRolesToUser.Result>(StatusCodes.Status200OK)
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
            return result.ToTypedResult();
        })
        .WithName(AssignBatchPermissionsToUser.Name)
        .WithSummary(AssignBatchPermissionsToUser.Summary)
        .WithDescription(AssignBatchPermissionsToUser.Description)
        .WithTags(PermissionManagementEndpoint.Tag, AssignBatchPermissionsToUser.Tag, Tag)
        .Produces<AssignBatchPermissionsToUser.Result>(StatusCodes.Status200OK)
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