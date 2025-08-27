using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace UseCases.Todos;

public static class TodoEndpoint
{
    internal const string Route = "api/todos";
    private const string Description = "Todo management API. Creating, updating, retrieving, and deleting todos.";
    private const string Summary = "Todos API";
    private const string Tag = "Todos";

    public static RouteGroupBuilder GetTodoEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapGroup(Route)
            .WithDescription(Description)
            .WithSummary(Summary)
            .WithTags(Tag);
    }
}