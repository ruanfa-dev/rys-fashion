using Carter;

using ErrorOr;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;
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
        RouteGroupBuilder group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(AccountEndpoint.Tag, Tag)
            .WithSummary(Summary)
            .WithDescription(Description);

        group.MapPost(ChangePassword.Route, async ([FromBody] ChangePassword.Param param, [FromServices] ISender mediator) =>
        {
            ChangePassword.Command command = new ChangePassword.Command(param);
            ErrorOr<Updated> result = await mediator.Send(command);
            ApiResponse<Updated> apiResponse = result.ToApiResponse("Password changed successfully");
            
            // Add password change metadata and links
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("profile", "/api/account/profile")
                    .WithLink("login", "/api/account/auth/login")
                    .WithLink("logout", "/api/account/auth/logout")
                    .WithMetadata("passwordChange", "successful")
                    .WithMetadata("changedAt", DateTime.UtcNow)
                    .WithMetadata("securityAction", true);
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(ChangePassword.Name)
        .WithSummary(ChangePassword.Summary)
        .WithDescription(ChangePassword.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        group.MapPost(ForgotPassword.Route, async ([FromBody] ForgotPassword.Param param, [FromServices] ISender mediator) =>
        {
            ForgotPassword.Command command = new ForgotPassword.Command(param);
            ErrorOr<ForgotPassword.Result> result = await mediator.Send(command);
            ApiResponse<ForgotPassword.Result> apiResponse = result.ToApiResponse("Password reset email sent successfully");
            
            // Add forgot password metadata and links
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("reset-password", "/api/account/password/reset")
                    .WithLink("login", "/api/account/auth/login")
                    .WithMetadata("passwordReset", "email-sent")
                    .WithMetadata("requestedAt", DateTime.UtcNow)
                    .WithMetadata("operation", "forgot-password");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(ForgotPassword.Name)
        .WithSummary(ForgotPassword.Summary)
        .WithDescription(ForgotPassword.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost(ResetPassword.Route, async ([FromBody] ResetPassword.Param param, [FromServices] ISender mediator) =>
        {
            ResetPassword.Command command = new ResetPassword.Command(param);
            ErrorOr<ResetPassword.Result> result = await mediator.Send(command);
            ApiResponse<ResetPassword.Result> apiResponse = result.ToApiResponse("Password reset successfully");
            
            // Add password reset metadata and links
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("login", "/api/account/auth/login")
                    .WithLink("profile", "/api/account/profile")
                    .WithMetadata("passwordReset", "successful")
                    .WithMetadata("resetAt", DateTime.UtcNow)
                    .WithMetadata("securityAction", true)
                    .WithMetadata("operation", "password-reset");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(ResetPassword.Name)
        .WithSummary(ResetPassword.Summary)
        .WithDescription(ResetPassword.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
