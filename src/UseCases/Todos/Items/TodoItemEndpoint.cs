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
            var apiResponse = result.ToApiResponseCreated("Todo item created successfully");
            
            // Add HATEOAS links for the created todo item
            if (apiResponse.IsSuccess)
            {
                var itemId = apiResponse.Data; // apiResponse.Data is an int (the ID)
                apiResponse
                    .WithLink("self", $"{Route}/{itemId}")
                    .WithLink("update", $"{Route}/{itemId}")
                    .WithLink("complete", $"{Route}/{itemId}/complete")
                    .WithLink("delete", $"{Route}/{itemId}");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(CreateTodoItem.Name)
        .WithSummary(CreateTodoItem.Summary)
        .WithDescription(CreateTodoItem.Description)
        .Produces<ApiResponse<int>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Testing.TodoItems.Create);

        group.MapGet("/{id:int}", async (int id, ISender mediator, CancellationToken cancellationToken) =>
        {
            var query = new GetTodoItemById.Query(id);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponse("Todo item retrieved successfully");
            
            // Add HATEOAS links for the todo item
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("update", $"{Route}/{id}")
                    .WithLink("complete", $"{Route}/{id}/complete")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("list", $"{TodoEndpoint.Route}/lists/{apiResponse.Data.ListId}");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetTodoItemById.Name)
        .WithSummary(GetTodoItemById.Summary)
        .WithDescription(GetTodoItemById.Description)
        .Produces<ApiResponse<TodoItemResult>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Testing.TodoItems.View);

        group.MapPut("/{id:int}", async (int id, TodoItemParam param, ISender mediator, CancellationToken cancellationToken) =>
        {
            var command = new UpdateTodoItem.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponseUpdated("Todo item updated successfully");
            
            // Add HATEOAS links for the updated todo item - use the id parameter since Updated doesn't have properties
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("complete", $"{Route}/{id}/complete")
                    .WithLink("delete", $"{Route}/{id}");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(UpdateTodoItem.Name)
        .WithSummary(UpdateTodoItem.Summary)
        .WithDescription(UpdateTodoItem.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Testing.TodoItems.Update);

        group.MapPatch("/{id:int}/complete", async (int id, ISender mediator, CancellationToken cancellationToken) =>
        {
            var command = new CompleteTodoItem.Command(id);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponseUpdated("Todo item completed successfully");
            
            // Add HATEOAS links and completion metadata - use the id parameter since Updated doesn't have properties
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("update", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithMetadata("completedAt", DateTime.UtcNow)
                    .WithMetadata("operation", "complete");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(CompleteTodoItem.Name)
        .WithSummary(CompleteTodoItem.Summary)
        .WithDescription(CompleteTodoItem.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Testing.TodoItems.Track);

        group.MapDelete("/{id:int}", async (int id, ISender mediator, CancellationToken cancellationToken) =>
        {
            var command = new DeleteTodoItem.Command(id);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponseDeleted("Todo item deleted successfully");
            
            // Add metadata for audit purposes
            apiResponse
                .WithMetadata("deletedAt", DateTime.UtcNow)
                .WithMetadata("deletedItemId", id)
                .WithMetadata("operation", "delete");
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(DeleteTodoItem.Name)
        .WithSummary(DeleteTodoItem.Summary)
        .WithDescription(DeleteTodoItem.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Testing.TodoItems.Delete);
    }
}