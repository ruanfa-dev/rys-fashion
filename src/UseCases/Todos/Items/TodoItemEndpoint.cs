using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
using UseCases.Common.Security.Authorization.Permissions;
using UseCases.Todos.Items.Common;
using UseCases.Todos.Items.Complete;
using UseCases.Todos.Items.Create;
using UseCases.Todos.Items.Delete;
using UseCases.Todos.Items.GetById;
using UseCases.Todos.Items.Update;

namespace UseCases.Todos.Items;

public sealed class TodoItemEndpoint : ICarterModule
{
    public const string Name = "TodoItem";
    public const string Tag = "Todo Items";
    public const string Summary = "Todo Items API";
    public const string Description = "Todo items management API. Allows creating, updating, retrieving, and deleting todo items.";

    public const string Route = $"{TodoEndpoint.Route}/items";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(TodoEndpoint.Tag, Tag)
            .WithSummary(Summary)
            .WithDescription(Description);

        group.MapPost("", async (TodoItemParam param, ISender mediator, CancellationToken cancellationToken) =>
        {
            var command = new CreateTodoItem.Command(param);
            var result = await mediator.Send(command, cancellationToken);
            return result.ToTypedResultCreated($"{Route}/{result.Value}");
        })
        .WithName(CreateTodoItem.Name)
        .WithSummary(CreateTodoItem.Summary)
        .WithDescription(CreateTodoItem.Description)
        .Produces<TodoItemResult>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .RequirePermission(Feature.Testing.TodoItems.Create);

        group.MapGet("/{id:int}", async (int id, ISender mediator, CancellationToken cancellationToken) =>
        {
            var query = new GetTodoItemById.Query(id);
            var result = await mediator.Send(query, cancellationToken);
            return result.ToTypedResult();
        })
        .WithName(GetTodoItemById.Name)
        .WithSummary(GetTodoItemById.Summary)
        .WithDescription(GetTodoItemById.Description)
        .Produces<TodoItemResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequirePermission(Feature.Testing.TodoItems.View);

        group.MapPut("/{id:int}", async (int id, TodoItemParam param, ISender mediator, CancellationToken cancellationToken) =>
        {
            var command = new UpdateTodoItem.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);
            return result.ToTypedResult();
        })
        .WithName(UpdateTodoItem.Name)
        .WithSummary(UpdateTodoItem.Summary)
        .WithDescription(UpdateTodoItem.Description)
        .Produces<TodoItemResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequirePermission(Feature.Testing.TodoItems.Update);

        group.MapPatch("/{id:int}/complete", async (int id, ISender mediator, CancellationToken cancellationToken) =>
        {
            var command = new CompleteTodoItem.Command(id);
            var result = await mediator.Send(command, cancellationToken);

            return result.ToTypedResult();
        })
        .WithName(CompleteTodoItem.Name)
        .WithSummary(CompleteTodoItem.Summary)
        .WithDescription(CompleteTodoItem.Description)
        .Produces<TodoItemResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .RequirePermission(Feature.Testing.TodoItems.Track);


        group.MapDelete("/{id:int}", async (int id, ISender mediator, CancellationToken cancellationToken) =>
        {
            var command = new DeleteTodoItem.Command(id);
            var result = await mediator.Send(command, cancellationToken);
            return result.ToTypedResultDeleted();
        })
        .WithName(DeleteTodoItem.Name)
        .WithSummary(DeleteTodoItem.Summary)
        .WithDescription(DeleteTodoItem.Description)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequirePermission(Feature.Testing.TodoItems.Delete);
    }
}