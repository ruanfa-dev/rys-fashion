using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using UseCases.Accounts.Password.Change;
using UseCases.Accounts.Password.Forgot;
using UseCases.Accounts.Password.Reset;
using UseCases.Common.Extensions;

namespace UseCases.Accounts.Password;
public sealed class PasswordEndpoint : ICarterModule
{
    internal const string Route = $"{AccountEndpoint.Route}/password";
    internal const string Tag = "Password";
    internal const string Description = "Endpoints for changing password, requesting password reset, and resetting password.";
    internal const string Summary = "Password API";
    internal const string Name = "Password";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(AccountEndpoint.Tag, Tag)
            .WithSummary(Summary)
            .WithDescription(Description);

        group.MapPost(ChangePassword.Route, async ([FromBody] ChangePassword.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ChangePassword.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResult();
        })
        .WithName(ChangePassword.Name)
        .WithSummary(ChangePassword.Summary)
        .WithDescription(ChangePassword.Description)
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost(ForgotPassword.Route, async ([FromBody] ForgotPassword.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ForgotPassword.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResult();
        })
        .WithName(ForgotPassword.Name)
        .WithSummary(ForgotPassword.Summary)
        .WithDescription(ForgotPassword.Description)
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost(ResetPassword.Route, async ([FromBody] ResetPassword.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ResetPassword.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResult();
        })
        .WithName(ResetPassword.Name)
        .WithSummary(ResetPassword.Summary)
        .WithDescription(ResetPassword.Description)
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
