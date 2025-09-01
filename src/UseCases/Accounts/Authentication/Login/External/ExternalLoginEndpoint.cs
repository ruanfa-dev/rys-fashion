using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using UseCases.Accounts.Authentication.Login.External.Exchange;
using UseCases.Accounts.Authentication.Login.External.GetConfig;
using UseCases.Accounts.Authentication.Login.External.Providers;
using UseCases.Accounts.Authentication.Login.External.Verify;
using UseCases.Common.Extensions;

namespace UseCases.Accounts.Authentication.Login.External;

public sealed class ExternalLoginEndpoint : ICarterModule
{
    internal const string Route = $"{AuthenticationEndpoint.Route}/external";
    internal const string Tag = "External Authentication";
    internal const string Description = "Token exchange endpoints for external authentication (OAuth handled by frontend)";
    internal const string Summary = "External Authentication Token Exchange API";
    internal const string Name = "ExternalAuthentication";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(AccountEndpoint.Tag, AuthenticationEndpoint.Tag, Tag)
            .WithSummary(Summary)
            .WithDescription(Description);

        // Get available external providers with frontend configuration
        group.MapGet("/providers", async ([FromServices] ISender mediator) =>
        {
            var query = new GetExternalProviders.Query();
            var result = await mediator.Send(query);

            return result.ToTypedResult();
        })
        .WithName(GetExternalProviders.Name)
        .WithSummary(GetExternalProviders.Summary)
        .WithDescription(GetExternalProviders.Description)
        .Produces<List<GetExternalProviders.Result>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Get OAuth configuration for frontend clients
        group.MapGet("/config/{provider}", async (
            [FromRoute] string provider,
            [FromServices] ISender mediator) =>
        {
            var query = new GetOAuthConfig.Query(provider);
            var result = await mediator.Send(query);

            return result.ToTypedResult();
        })
        .WithName(GetOAuthConfig.Name)
        .WithSummary(GetOAuthConfig.Summary)
        .WithDescription(GetOAuthConfig.Description)
        .Produces<GetOAuthConfig.Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Exchange external provider token for application tokens
        group.MapPost("/token/exchange/{provider}", async (
            [FromRoute] string provider,
            [FromBody] ExchangeExternalToken.Command command,
            [FromServices] ISender mediator) =>
        {
            var commandWithProvider = command with { Provider = provider };
            var result = await mediator.Send(commandWithProvider);

            return result.ToTypedResult();
        })
        .WithName(ExchangeExternalToken.Name)
        .WithSummary(ExchangeExternalToken.Summary)
        .WithDescription(ExchangeExternalToken.Description)
        .Produces<ExchangeExternalToken.Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Verify external token without creating session
        group.MapPost("/token/verify/{provider}", async (
            [FromRoute] string provider,
            [FromBody] VerifyExternalToken.Command command,
            [FromServices] ISender mediator) =>
        {
            var commandWithProvider = command with { Provider = provider };
            var result = await mediator.Send(commandWithProvider);

            return result.ToTypedResult();
        })
        .WithName(VerifyExternalToken.Name)
        .WithSummary(VerifyExternalToken.Summary)
        .WithDescription(VerifyExternalToken.Description)
        .Produces<VerifyExternalToken.Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}