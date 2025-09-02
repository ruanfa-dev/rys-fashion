using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using UseCases.Accounts.Authentication.LogAllOut;
using UseCases.Accounts.Authentication.Login.Password;
using UseCases.Accounts.Authentication.LogOut;
using UseCases.Accounts.Authentication.Register;
using UseCases.Accounts.Authentication.Sessions;
using UseCases.Accounts.Sessions.Get;
using UseCases.Common.Extensions;

namespace UseCases.Accounts.Authentication;

public sealed class AuthenticationEndpoint : ICarterModule
{
    internal const string Route = $"{AccountEndpoint.Route}/auth";
    internal const string Tag = "Authentication";
    internal const string Description = "Authentication endpoints for user login, registration, and token management.";
    internal const string Summary = "Authentication API";
    internal const string Name = "Authentication";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(AccountEndpoint.Tag, Tag)
            .WithSummary(Summary)
            .WithDescription(Description);

        // Login with password
        group.MapPost(LoginWithPassword.Route, async ([FromBody] LoginWithPassword.Param param, [FromServices] ISender mediator) =>
        {
            var command = new LoginWithPassword.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResult();
        })
        .WithName(LoginWithPassword.Name)
        .WithSummary(LoginWithPassword.Summary)
        .WithDescription(LoginWithPassword.Description)
        .Produces<LoginWithPassword.Result>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Customer registration
        group.MapPost(CustomerRegister.Route, async ([FromBody] CustomerRegister.Param param, [FromServices] ISender sender) =>
        {
            var command = new CustomerRegister.Command(param);
            var result = await sender.Send(command);
            return result.ToTypedResult();
        })
        .WithName(CustomerRegister.Name)
        .WithSummary(CustomerRegister.Summary)
        .WithDescription(CustomerRegister.Description)
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Get current session
        group.MapGet(GetSession.Route, async ([FromServices] ISender mediator) =>
        {
            var query = new GetSession.Query();
            var result = await mediator.Send(query);
            return result.ToTypedResult();
        })
        .WithName(GetSession.Name)
        .WithSummary(GetSession.Summary)
        .WithDescription(GetSession.Description)
        .Produces<AccountSessionResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        // Logout current session
        group.MapPost(Logout.Route, async ([FromBody] Logout.Param param, [FromServices] ISender mediator) =>
        {
            var command = new Logout.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResultDeleted();
        })
        .WithName(Logout.Name)
        .WithSummary(Logout.Summary)
        .WithDescription(Logout.Description)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        // Logout from all devices
        group.MapPost(LogoutFromAll.Route, async ([FromBody] LogoutFromAll.Param param, [FromServices] ISender mediator) =>
        {
            var command = new LogoutFromAll.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResultDeleted();
        })
        .WithName(LogoutFromAll.Name)
        .WithSummary(LogoutFromAll.Summary)
        .WithDescription(LogoutFromAll.Description)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();
    }
}
