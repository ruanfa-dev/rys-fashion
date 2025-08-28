using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models.Filter;
using SharedKernel.Models.Paging;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

using UseCases.Common.Extensions;
using UseCases.Common.Security.Authentication.Attributes;
using UseCases.Common.Security.Authorization.Permissions;
using UseCases.Todos.Lists.Common;
using UseCases.Todos.Lists.Create;
using UseCases.Todos.Lists.Delete;
using UseCases.Todos.Lists.GetById;
using UseCases.Todos.Lists.GetList;
using UseCases.Todos.Lists.Update;

namespace UseCases.Todos.Lists;

public sealed class TodoListEndpoint : ICarterModule
{
    public const string Name = "TodoLists";
    public const string Tags = "Todo Lists";
    public const string Summary = "Todo Lists API";
    public const string Description = "Todo lists management API. Allows creating, updating, retrieving, and deleting todo lists.";

    public const string Route = $"{TodoEndpoint.Route}/lists";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(Tags)
            .WithSummary(Summary)
            .WithDescription(Description);

        group.MapPost("", async (TodoListParam param, ISender mediator) =>
        {
            var command = new CreateTodoList.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResultCreated($"{Route}/{result.Value?.Id}");
        })
        .WithName(CreateTodoList.Name)
        .WithSummary(CreateTodoList.Summary)
        .WithDescription(CreateTodoList.Description)
        .Produces<TodoListResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .RequirePermission(Feature.Testing.Todo.Lists.Create);

        group.MapGet("/", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator) =>
        {
            var query = new GetTodoListPagedList.Query
            {
                Search = search,
                Pagination = pagination,
                Sort = sort,
                Filter = filter
            };
            var result = await mediator.Send(query);
            return result.ToTypedResult();
        })
        .WithName(GetTodoListPagedList.Name)
        .WithSummary(GetTodoListPagedList.Summary)
        .WithDescription(GetTodoListPagedList.Description)
        .Produces<List<TodoListResult>>(StatusCodes.Status200OK)
        .RequirePermission(Feature.Testing.Todo.Lists.List);

        group.MapGet("/{id:int}", async (int id, ISender mediator) =>
        {
            var query = new GetTodoListById.Query(id);
            var result = await mediator.Send(query);
            return result.ToTypedResult();
        })
        .WithName(GetTodoListById.Name)
        .WithSummary(GetTodoListById.Summary)
        .WithDescription(GetTodoListById.Description)
        .Produces<TodoListResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequirePermission(Feature.Testing.Todo.Lists.View);

        group.MapPut("/{id:int}", async (int id, TodoListParam param, ISender mediator) =>
        {
            var command = new UpdateTodoList.Command(id, param);
            var result = await mediator.Send(command);
            return result.ToTypedResult();
        })
        .WithName(UpdateTodoList.Name)
        .WithSummary(UpdateTodoList.Summary)
        .WithDescription(UpdateTodoList.Description)
        .Produces<TodoListResult>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .ProducesValidationProblem()
        .RequirePermission(Feature.Testing.Todo.Lists.View);

        group.MapDelete("/{id:int}", async (int id, ISender mediator) =>
        {
            var command = new DeleteTodoList.Command(id);
            var result = await mediator.Send(command);
            return result.ToTypedResultDeleted();
        })
        .WithName(DeleteTodoList.Name)
        .WithSummary(DeleteTodoList.Summary)
        .WithDescription(DeleteTodoList.Description)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}