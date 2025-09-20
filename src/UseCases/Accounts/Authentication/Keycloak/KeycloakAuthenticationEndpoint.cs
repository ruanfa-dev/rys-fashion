using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;

using UseCases.Common.Extensions;
using UseCases.Accounts.Authentication.Login;
using UseCases.Accounts.Authentication.RefreshTokens;
using UseCases.Accounts.Authentication.LogOut;
using UseCases.Accounts.Authentication.GetUserInfo;

namespace UseCases.Accounts.Authentication.Keycloak;

public sealed class KeycloakAuthenticationEndpoint : ICarterModule
{
    internal const string Route = "api/auth";
    internal const string Tag = "Authentication";
    internal const string Description = "Authentication endpoints supporting both Keycloak and local JWT tokens";
    internal const string Summary = "Unified Authentication API";
    internal const string Name = "Authentication";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(Tag)
            .WithSummary(Summary)
            .WithDescription(Description);

        // Login with Keycloak
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand.Command(request.Username, request.Password);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var loginResult = result.Value;
            var apiResponse = ApiResponse<LoginCommand.LoginResult>.Success(loginResult, "Login successful");

            // Add authentication metadata
            apiResponse
                .WithMetadata("source", "keycloak")
                .WithMetadata("authenticatedAt", DateTime.UtcNow)
                .WithMetadata("tokenType", "Bearer")
                .WithMetadata("scope", "rys-fashion-api");

            return TypedResults.Ok(apiResponse);
        })
        .WithName("KeycloakLogin")
        .WithSummary(LoginCommand.Summary)
        .WithDescription(LoginCommand.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<LoginCommand.LoginResult>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Refresh token
        group.MapPost("/refresh", async (
            [FromBody] RefreshTokenRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new RefreshTokenCommand.Command(request.RefreshToken);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var refreshResult = result.Value;
            var apiResponse = ApiResponse<RefreshTokenCommand.RefreshTokenResult>.Success(refreshResult, "Token refreshed successfully");

            // Add token refresh metadata
            apiResponse
                .WithMetadata("source", "keycloak")
                .WithMetadata("refreshedAt", DateTime.UtcNow)
                .WithMetadata("tokenType", "Bearer")
                .WithMetadata("scope", "rys-fashion-api");

            return TypedResults.Ok(apiResponse);
        })
        .WithName("RefreshKeycloakToken")
        .WithSummary(RefreshTokenCommand.Summary)
        .WithDescription(RefreshTokenCommand.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<RefreshTokenCommand.RefreshTokenResult>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Logout
        group.MapPost("/logout", async (
            [FromBody] LogoutRequest request,
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            // Get token from Authorization header if not provided in request
            var token = request.RefreshToken;
            if (string.IsNullOrEmpty(token))
            {
                var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    token = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            var command = new LogoutCommand.Command(token);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var logoutResult = result.Value;
            var apiResponse = ApiResponse<LogoutCommand.LogoutResult>.Success(logoutResult, "Logout successful");

            // Add logout metadata
            apiResponse
                .WithMetadata("source", "keycloak")
                .WithMetadata("loggedOutAt", DateTime.UtcNow);

            return TypedResults.Ok(apiResponse);
        })
        .WithName("KeycloakLogout")
        .WithSummary(LogoutCommand.Summary)
        .WithDescription(LogoutCommand.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<LogoutCommand.LogoutResult>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Get user info
        group.MapGet("/userinfo", async (
            [FromServices] IMediator mediator,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            // Get token from Authorization header
            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                var error = ErrorOr.Error.Unauthorized("Token.Missing", "Authorization token is required");
                return error.ToApiResponse();
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var query = new GetUserInfoQuery.Query(token);
            var result = await mediator.Send(query, cancellationToken);

            if (result.IsError)
            {
                return result.Errors.ToApiResponse();
            }

            var userInfoResult = result.Value;
            var apiResponse = ApiResponse<GetUserInfoQuery.UserInfoResult>.Success(userInfoResult, "User information retrieved successfully");

            // Add user info metadata
            apiResponse
                .WithMetadata("source", "keycloak")
                .WithMetadata("retrievedAt", DateTime.UtcNow)
                .WithMetadata("userId", userInfoResult.Subject);

            return TypedResults.Ok(apiResponse);
        })
        .WithName("GetKeycloakUserInfo")
        .WithSummary(GetUserInfoQuery.Summary)
        .WithDescription(GetUserInfoQuery.Description)
        .WithTags(Tag)
        .RequireAuthorization()
        .Produces<ApiResponse<GetUserInfoQuery.UserInfoResult>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}

// Request Models
public record LoginRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public record LogoutRequest
{
    public string? RefreshToken { get; init; }
}