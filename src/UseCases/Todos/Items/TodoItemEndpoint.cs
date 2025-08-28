using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using UseCases.Common.Extensions;
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
            .WithTags(Tag)
            .WithSummary(Summary)
            .WithDescription(Description);

        group.MapPost("", async (TodoItemParam param, ISender mediator) =>
        {
            var command = new CreateTodoItem.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResultCreated($"{Route}/{result.Value}");
        })
        .WithName(CreateTodoItem.Name)
        .WithSummary(CreateTodoItem.Summary)
        .WithDescription(CreateTodoItem.Description)
        .Produces<TodoItemResult>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .RequireAuthorization(Feature.Testing.Todo.Items.Create);

        group.MapGet("/{id:int}", async (int id, ISender mediator) =>
        {
            var query = new GetTodoItemById.Query(id);
            var result = await mediator.Send(query);
            return result.ToTypedResult();
        })
        .WithName(GetTodoItemById.Name)
        .WithSummary(GetTodoItemById.Summary)
        .WithDescription(GetTodoItemById.Description)
        .Produces<TodoItemResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequireAuthorization(Feature.Testing.Todo.Items.View);

        group.MapPut("/{id:int}", async (int id, TodoItemParam param, ISender mediator) =>
        {
            var command = new UpdateTodoItem.Command(id, param);
            var result = await mediator.Send(command);
            return result.ToTypedResult();
        })
        .WithName(UpdateTodoItem.Name)
        .WithSummary(UpdateTodoItem.Summary)
        .WithDescription(UpdateTodoItem.Description)
        .Produces<TodoItemResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequireAuthorization(Feature.Testing.Todo.Items.Update);

        group.MapPatch("/{id:int}/complete", async (int id, ISender mediator) =>
        {
            var command = new CompleteTodoItem.Command(id);
            var result = await mediator.Send(command);

            return result.ToTypedResult();
        })
        .WithName(CompleteTodoItem.Name)
        .WithSummary(CompleteTodoItem.Summary)
        .WithDescription(CompleteTodoItem.Description)
        .Produces<TodoItemResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .RequireAuthorization(Feature.Testing.Todo.Items.Track);


        group.MapDelete("/{id:int}", async (int id, ISender mediator) =>
        {
            var command = new DeleteTodoItem.Command(id);
            var result = await mediator.Send(command);
            return result.ToTypedResultDeleted();
        })
        .WithName(DeleteTodoItem.Name)
        .WithSummary(DeleteTodoItem.Summary)
        .WithDescription(DeleteTodoItem.Description)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequireAuthorization(Feature.Testing.Todo.Items.Delete);
    }
}