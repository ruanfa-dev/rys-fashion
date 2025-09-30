using Carter;

using ErrorOr;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;
using SharedKernel.Models.Filter;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Paging;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
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
    public const string Tag = "Todo Lists";
    public const string Summary = "Todo Lists API";
    public const string Description = "Todo lists management API. Allows creating, updating, retrieving, and deleting todo lists.";

    public const string Route = $"{TodoEndpoint.Route}/lists";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(TodoEndpoint.Tag, Tag)
            .WithSummary(Summary)
            .WithDescription(Description);

        group.MapPost("", async (TodoListParam param, ISender mediator, CancellationToken cancellationToken) =>
        {
            CreateTodoList.Command command = new CreateTodoList.Command(param);
            ErrorOr<TodoListResult> result = await mediator.Send(command, cancellationToken);
            ApiResponse<TodoListResult> apiResponse = result.ToApiResponseCreated("Todo list created successfully");

            // Add HATEOAS links for the created todo list
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("update", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("delete", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("items", $"{TodoEndpoint.Route}/items?listId={apiResponse.Data.Id}")
                    .WithLink("all-lists", Route);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(CreateTodoList.Name)
        .WithSummary(CreateTodoList.Summary)
        .WithDescription(CreateTodoList.Description)
        .Produces<ApiResponse<TodoListResult>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Testing.TodoLists.Create);

        group.MapGet("/", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            GetTodoListPagedList.Param param = new GetTodoListPagedList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            GetTodoListPagedList.Query query = new GetTodoListPagedList.Query(param);
            ErrorOr<PagedList<GetTodoListPagedList.Result>> result = await mediator.Send(query, cancellationToken);
            ApiResponse<List<GetTodoListPagedList.Result>> apiResponse = result.ToApiResponsePaged("Todo lists retrieved successfully");

            // Add HATEOAS links for pagination
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                // Use PageIndex instead of PageNumber (based on PagingParams structure)
                int currentPage = (pagination.PageIndex ?? 0) + 1; // Convert 0-based index to 1-based page number
                int pageSize = pagination.PageSize ?? 10;

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
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetTodoListPagedList.Name)
        .WithSummary(GetTodoListPagedList.Summary)
        .WithDescription(GetTodoListPagedList.Description)
        .Produces<ApiResponse<List<GetTodoListPagedList.Result>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Testing.TodoLists.List);

        group.MapGet("/{id:int}", async (int id, ISender mediator, CancellationToken cancellationToken) =>
        {
            GetTodoListById.Query query = new GetTodoListById.Query(id);
            ErrorOr<TodoListResult> result = await mediator.Send(query, cancellationToken);
            ApiResponse<TodoListResult> apiResponse = result.ToApiResponse("Todo list retrieved successfully");

            // Add HATEOAS links for the todo list
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("update", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("items", $"{TodoEndpoint.Route}/items?listId={id}")
                    .WithLink("all-lists", Route);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetTodoListById.Name)
        .WithSummary(GetTodoListById.Summary)
        .WithDescription(GetTodoListById.Description)
        .Produces<ApiResponse<TodoListResult>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Testing.TodoLists.View);

        group.MapPut("/{id:int}", async (int id, TodoListParam param, ISender mediator, CancellationToken cancellationToken) =>
        {
            UpdateTodoList.Command command = new UpdateTodoList.Command(id, param);
            ErrorOr<Updated> result = await mediator.Send(command, cancellationToken);
            ApiResponse<Updated> apiResponse = result.ToApiResponse("Todo list updated successfully");

            // Add HATEOAS links for the updated todo list - use the id parameter since Updated doesn't have properties
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("items", $"{TodoEndpoint.Route}/items?listId={id}")
                    .WithLink("all-lists", Route);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(UpdateTodoList.Name)
        .WithSummary(UpdateTodoList.Summary)
        .WithDescription(UpdateTodoList.Description)
        .Produces<ApiResponse<TodoListResult>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Testing.TodoLists.Update);

        group.MapDelete("/{id:int}", async (int id, ISender mediator, CancellationToken cancellationToken) =>
        {
            DeleteTodoList.Command command = new DeleteTodoList.Command(id);
            ErrorOr<Deleted> result = await mediator.Send(command, cancellationToken);
            ApiResponse apiResponse = result.ToApiResponseDeleted("Todo list deleted successfully");

            // Add metadata for audit purposes
            apiResponse
                .WithMetadata("deletedAt", DateTime.UtcNow)
                .WithMetadata("deletedListId", id)
                .WithMetadata("operation", "delete")
                .WithLink("all-lists", Route);

            return TypedResults.Ok(apiResponse);
        })
        .WithName(DeleteTodoList.Name)
        .WithSummary(DeleteTodoList.Summary)
        .WithDescription(DeleteTodoList.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Testing.TodoLists.Delete);
    }
}