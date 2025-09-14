using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;
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
            var apiResponse = result.ToApiResponse("Email change request sent successfully");
            
            // Add email change metadata and links
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("profile", "/api/account/profile")
                    .WithLink("resend-confirmation", "/api/account/email/resend")
                    .WithLink("confirm-email", "/api/account/email/confirm")
                    .WithMetadata("emailChangeOperation", "initiated")
                    .WithMetadata("requiresConfirmation", true)
                    .WithMetadata("requestedAt", DateTime.UtcNow);
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(ChangeEmail.Name)
        .WithSummary(ChangeEmail.Summary)
        .WithDescription(ChangeEmail.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        group.MapPost(ConfirmEmail.Route, async ([FromBody] ConfirmEmail.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ConfirmEmail.Command(param);
            var result = await mediator.Send(command);
            var apiResponse = result.ToApiResponse("Email confirmed successfully");
            
            // Add email confirmation metadata and links
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("profile", "/api/account/profile")
                    .WithLink("login", "/api/account/auth/login")
                    .WithLink("dashboard", "/")
                    .WithMetadata("emailConfirmation", "successful")
                    .WithMetadata("confirmedAt", DateTime.UtcNow)
                    .WithMetadata("accountStatus", "active");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(ConfirmEmail.Name)
        .WithSummary(ConfirmEmail.Summary)
        .WithDescription(ConfirmEmail.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost(ResendEmailConfirmation.Route, async ([FromBody] ResendEmailConfirmation.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ResendEmailConfirmation.Command(param);
            var result = await mediator.Send(command);
            var apiResponse = result.ToApiResponse("Email confirmation resent successfully");
            
            // Add resend confirmation metadata and links
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("confirm-email", "/api/account/email/confirm")
                    .WithLink("profile", "/api/account/profile")
                    .WithMetadata("resendOperation", "successful")
                    .WithMetadata("resentAt", DateTime.UtcNow)
                    .WithMetadata("operation", "email-resend");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(ResendEmailConfirmation.Name)
        .WithSummary(ResendEmailConfirmation.Summary)
        .WithDescription(ResendEmailConfirmation.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
