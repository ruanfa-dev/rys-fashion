using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;
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
            var apiResponse = result.ToApiResponse("Phone change request sent successfully");
            
            // Add phone change metadata and links
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("confirm-phone", "/api/account/phone/confirm")
                    .WithLink("resend-verification", "/api/account/phone/resend")
                    .WithLink("profile", "/api/account/profile")
                    .WithMetadata("phoneChangeOperation", "initiated")
                    .WithMetadata("requiresVerification", true)
                    .WithMetadata("requestedAt", DateTime.UtcNow);
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(ChangePhone.Name)
        .WithSummary(ChangePhone.Summary)
        .WithDescription(ChangePhone.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        group.MapPost(ConfirmPhoneChange.Route, async ([FromBody] ConfirmPhoneChange.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ConfirmPhoneChange.Command(param);
            var result = await mediator.Send(command);
            var apiResponse = result.ToApiResponse("Phone number confirmed successfully");
            
            // Add phone confirmation metadata and links
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("profile", "/api/account/profile")
                    .WithLink("change-phone", "/api/account/phone/change")
                    .WithMetadata("phoneConfirmation", "successful")
                    .WithMetadata("confirmedAt", DateTime.UtcNow)
                    .WithMetadata("securityAction", true);
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(ConfirmPhoneChange.Name)
        .WithSummary(ConfirmPhoneChange.Summary)
        .WithDescription(ConfirmPhoneChange.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        group.MapPost(ResendPhoneVerification.Route, async ([FromBody] ResendPhoneVerification.Param param, [FromServices] ISender mediator) =>
        {
            var command = new ResendPhoneVerification.Command(param);
            var result = await mediator.Send(command);
            var apiResponse = result.ToApiResponse("Phone verification code resent successfully");
            
            // Add resend verification metadata and links
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("confirm-phone", "/api/account/phone/confirm")
                    .WithLink("profile", "/api/account/profile")
                    .WithMetadata("resendOperation", "successful")
                    .WithMetadata("resentAt", DateTime.UtcNow)
                    .WithMetadata("operation", "phone-verification-resend");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(ResendPhoneVerification.Name)
        .WithSummary(ResendPhoneVerification.Summary)
        .WithDescription(ResendPhoneVerification.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();
    }
}
