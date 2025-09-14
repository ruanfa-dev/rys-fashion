using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using UseCases.Accounts.Email.Change;
using UseCases.Accounts.Email.Confirm;
using UseCases.Accounts.Email.Resend;
using UseCases.Common.Extensions;

namespace UseCases.Accounts.Email;
public sealed class EmailEndpoint : ICarterModule
{
    internal const string Route = $"{AccountEndpoint.Route}";
    internal const string Tag = "Email";
    internal const string Description = "Endpoints for changing email, confirming email, and resending email confirmation.";
    internal const string Summary = "Email API";
    internal const string Name = "Email";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(AccountEndpoint.Tag, Tag)
            .WithSummary(Summary)
            .WithDescription(Description);

        group.MapPost(ChangeEmail.Route, async ([FromBody] ChangeEmail.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ChangeEmail.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResult();
        })
        .WithName(ChangeEmail.Name)
        .WithSummary(ChangeEmail.Summary)
        .WithDescription(ChangeEmail.Description)
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost(ConfirmEmail.Route, async ([FromBody] ConfirmEmail.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ConfirmEmail.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResult();
        })
        .WithName(ConfirmEmail.Name)
        .WithSummary(ConfirmEmail.Summary)
        .WithDescription(ConfirmEmail.Description)
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost(ResendEmailConfirmation.Route, async ([FromBody] ResendEmailConfirmation.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ResendEmailConfirmation.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResult();
        })
        .WithName(ResendEmailConfirmation.Name)
        .WithSummary(ResendEmailConfirmation.Summary)
        .WithDescription(ResendEmailConfirmation.Description)
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
