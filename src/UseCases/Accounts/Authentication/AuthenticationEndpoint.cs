using Carter;

using ErrorOr;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;
using UseCases.Accounts.Authentication.LogAllOut;
using UseCases.Accounts.Authentication.Login.Password;
using UseCases.Accounts.Authentication.LogOut;
using UseCases.Accounts.Authentication.Register;
using UseCases.Accounts.Authentication.Sessions;
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
        RouteGroupBuilder group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(AccountEndpoint.Tag, Tag)
            .WithSummary(Summary)
            .WithDescription(Description);

        // Login with password
        group.MapPost(LoginWithPassword.Route, async ([FromBody] LoginWithPassword.Param param, [FromServices] ISender mediator) =>
        {
            LoginWithPassword.Command command = new LoginWithPassword.Command(param);
            ErrorOr<LoginWithPassword.Result> result = await mediator.Send(command);
            ApiResponse<LoginWithPassword.Result> apiResponse = result.ToApiResponse("User logged in successfully");
            
            // Add authentication-related metadata and links
            if (apiResponse is { IsSuccess: true, Data: not null })
            {
                apiResponse
                    .WithLink("profile", "/api/account/profile")
                    .WithLink("logout", $"{Route}/logout")
                    .WithLink("session", $"{Route}/session")
                    .WithMetadata("loginMethod", "password")
                    .WithMetadata("loginTime", DateTime.UtcNow);
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(LoginWithPassword.Name)
        .WithSummary(LoginWithPassword.Summary)
        .WithDescription(LoginWithPassword.Description)
        .Produces<ApiResponse<LoginWithPassword.Result>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Customer registration
        group.MapPost(CustomerRegister.Route, async ([FromBody] CustomerRegister.Param param, [FromServices] ISender sender) =>
        {
            CustomerRegister.Command command = new CustomerRegister.Command(param);
            ErrorOr<Guid> result = await sender.Send(command);
            ApiResponse<Guid> apiResponse = result.ToApiResponseCreated("Account created successfully");
            
            // Add registration-related metadata and links
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("login", $"{Route}/login")
                    .WithLink("confirm-email", "/api/account/email/confirm")
                    .WithMetadata("registrationMethod", "email")
                    .WithMetadata("registrationTime", DateTime.UtcNow)
                    .WithMetadata("requiresEmailConfirmation", true);
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(CustomerRegister.Name)
        .WithSummary(CustomerRegister.Summary)
        .WithDescription(CustomerRegister.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Get current session
        group.MapGet(GetSession.Route, async ([FromServices] ISender mediator) =>
        {
            GetSession.Query query = new GetSession.Query();
            ErrorOr<AccountSessionResult> result = await mediator.Send(query);
            ApiResponse<AccountSessionResult> apiResponse = result.ToApiResponse("Session information retrieved successfully");
            
            // Add session-related metadata and links
            if (apiResponse is { IsSuccess: true, Data: not null })
            {
                apiResponse
                    .WithLink("profile", "/api/account/profile")
                    .WithLink("logout", $"{Route}/logout")
                    .WithLink("logout-all", $"{Route}/logout-all")
                    .WithMetadata("sessionType", "active")
                    .WithMetadata("retrievedAt", DateTime.UtcNow);
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetSession.Name)
        .WithSummary(GetSession.Summary)
        .WithDescription(GetSession.Description)
        .Produces<ApiResponse<AccountSessionResult>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        // Logout current session
        group.MapPost(Logout.Route, async ([FromBody] Logout.Param param, [FromServices] ISender mediator) =>
        {
            Logout.Command command = new Logout.Command(param);
            ErrorOr<Deleted> result = await mediator.Send(command);
            ApiResponse apiResponse = result.ToApiResponseDeleted("Successfully logged out");
            
            // Add logout metadata and links
            apiResponse
                .WithLink("login", $"{Route}/login")
                .WithLink("register", $"{Route}/register")
                .WithMetadata("logoutTime", DateTime.UtcNow)
                .WithMetadata("logoutType", "single-session")
                .WithMetadata("operation", "logout");
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(Logout.Name)
        .WithSummary(Logout.Summary)
        .WithDescription(Logout.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        // Logout from all devices
        group.MapPost(LogoutFromAll.Route, async ([FromBody] LogoutFromAll.Param param, [FromServices] ISender mediator) =>
        {
            LogoutFromAll.Command command = new LogoutFromAll.Command(param);
            ErrorOr<Deleted> result = await mediator.Send(command);
            ApiResponse apiResponse = result.ToApiResponseDeleted("Successfully logged out from all devices");
            
            // Add logout-all metadata and links
            apiResponse
                .WithLink("login", $"{Route}/login")
                .WithLink("register", $"{Route}/register")
                .WithMetadata("logoutTime", DateTime.UtcNow)
                .WithMetadata("logoutType", "all-sessions")
                .WithMetadata("operation", "logout-all");
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(LogoutFromAll.Name)
        .WithSummary(LogoutFromAll.Summary)
        .WithDescription(LogoutFromAll.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();
    }
}
