using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using UseCases.Accounts.Phone.Change;
using UseCases.Accounts.Phone.Confirm;
using UseCases.Accounts.Phone.Resend;
using UseCases.Common.Extensions;

namespace UseCases.Accounts.Phone;
public sealed class PhoneEndpoint : ICarterModule
{
    internal const string Route = $"{AccountEndpoint.Route}/phone";
    internal const string Tag = "Phone";
    internal const string Description = "Endpoints for changing phone, confirming phone change, and resending phone confirmation.";
    internal const string Summary = "Phone API";
    internal const string Name = "Phone";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(AccountEndpoint.Tag, Tag)
            .WithSummary(Summary)
            .WithDescription(Description);

        group.MapPost(ChangePhone.Route, async ([FromBody] ChangePhone.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ChangePhone.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResult();
        })
        .WithName(ChangePhone.Name)
        .WithSummary(ChangePhone.Summary)
        .WithDescription(ChangePhone.Description)
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost(ConfirmPhoneChange.Route, async ([FromBody] ConfirmPhoneChange.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ConfirmPhoneChange.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResult();
        })
        .WithName(ConfirmPhoneChange.Name)
        .WithSummary(ConfirmPhoneChange.Summary)
        .WithDescription(ConfirmPhoneChange.Description)
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost(ResendPhoneVerification.Route, async ([FromBody] ResendPhoneVerification.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ResendPhoneVerification.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResult();
        })
        .WithName(ResendPhoneVerification.Name)
        .WithSummary(ResendPhoneVerification.Summary)
        .WithDescription(ResendPhoneVerification.Description)
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
