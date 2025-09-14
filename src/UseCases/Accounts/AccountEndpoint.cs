using Carter;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace UseCases.Accounts;
public sealed class AccountEndpoint : ICarterModule
{
    internal const string Route = "api/account";
    internal const string Tag = "Account";
    internal const string Description = "Account management API. Allows user authentication, profile management, and account settings.";
    internal const string Summary = "Account API";
    internal const string Name = "Account";
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Route)
            .WithName(Name)
            .WithDescription(Description)
            .WithSummary(Summary)
            .WithTags(Tag);
    }
}
