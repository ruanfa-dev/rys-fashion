using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using UseCases.Accounts.Profile.Common;
using UseCases.Accounts.Profile.Get;
using UseCases.Accounts.Profile.Update;
using UseCases.Common.Extensions;

namespace UseCases.Accounts.Profile;
public sealed class ProfileEndpoint : ICarterModule
{
    internal const string Route = $"{AccountEndpoint.Route}/profile";
    internal const string Tag = "Profile";
    internal const string Description = "Endpoints for getting and updating user profile.";
    internal const string Summary = "Profile API";
    internal const string Name = "Profile";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(AccountEndpoint.Tag, Tag)
            .WithSummary(Summary)
            .WithDescription(Description);

        group.MapGet(GetProfile.Route, async ([FromServices] ISender mediator) =>
        {
            var query = new GetProfile.Query();
            var result = await mediator.Send(query);
            return result.ToTypedResult();
        })
        .WithName(GetProfile.Name)
        .WithSummary(GetProfile.Summary)
        .WithDescription(GetProfile.Description)
        .Produces<AccountProfileResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPut(UpdateProfile.Route, async ([FromBody] AccountProfileParam param, [FromServices] ISender mediator) =>
        {
            var command = new UpdateProfile.Command(param);
            var result = await mediator.Send(command);
            return result.ToTypedResultNoContent();
        })
        .WithName(UpdateProfile.Name)
        .WithSummary(UpdateProfile.Summary)
        .WithDescription(UpdateProfile.Description)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
