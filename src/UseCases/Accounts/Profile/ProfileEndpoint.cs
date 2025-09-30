using Carter;

using ErrorOr;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;
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
        RouteGroupBuilder group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(AccountEndpoint.Tag, Tag)
            .WithSummary(Summary)
            .WithDescription(Description)
            .RequireAuthorization(); // All profile endpoints require authentication

        group.MapGet(GetProfile.Route, async ([FromServices] ISender mediator) =>
        {
            GetProfile.Query query = new GetProfile.Query();
            ErrorOr<AccountProfileResult> result = await mediator.Send(query);
            ApiResponse<AccountProfileResult> apiResponse = result.ToApiResponse("Profile retrieved successfully");
            
            // Add profile-related metadata and links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", Route)
                    .WithLink("update", Route)
                    .WithLink("change-email", "/api/account/email/change")
                    .WithLink("change-password", "/api/account/password/change")
                    .WithLink("session", "/api/account/auth/session")
                    .WithMetadata("profileType", "personal")
                    .WithMetadata("retrievedAt", DateTime.UtcNow);
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetProfile.Name)
        .WithSummary(GetProfile.Summary)
        .WithDescription(GetProfile.Description)
        .Produces<ApiResponse<AccountProfileResult>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPut(UpdateProfile.Route, async ([FromBody] AccountProfileParam param, [FromServices] ISender mediator) =>
        {
            UpdateProfile.Command command = new UpdateProfile.Command(param);
            ErrorOr<Updated> result = await mediator.Send(command);
            ApiResponse<Updated> apiResponse = result.ToApiResponse("Profile updated successfully");
            
            // Add profile update metadata and links
            if (apiResponse.IsSuccess)
            {
                apiResponse
                    .WithLink("self", Route)
                    .WithLink("get-profile", Route)
                    .WithLink("change-email", "/api/account/email/change")
                    .WithLink("change-password", "/api/account/password/change")
                    .WithMetadata("profileUpdate", "successful")
                    .WithMetadata("updatedAt", DateTime.UtcNow)
                    .WithMetadata("operation", "profile-update");
            }
            
            return TypedResults.Ok(apiResponse);
        })
        .WithName(UpdateProfile.Name)
        .WithSummary(UpdateProfile.Summary)
        .WithDescription(UpdateProfile.Description)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
