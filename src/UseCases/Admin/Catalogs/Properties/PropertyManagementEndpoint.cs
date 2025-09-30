using Carter;

using ErrorOr;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;
using SharedKernel.Models.Filter;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Paging;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

using UseCases.Admin.Catalogs.Properties.Create;
using UseCases.Admin.Catalogs.Properties.Delete;
using UseCases.Admin.Catalogs.Properties.Get.Id;
using UseCases.Admin.Catalogs.Properties.Get.OptionList;
using UseCases.Admin.Catalogs.Properties.Get.PagedList;
using UseCases.Admin.Catalogs.Properties.Update;
using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
using UseCases.Common.Security.Authorization.Permissions;

namespace UseCases.Admin.Catalogs.Properties;

public sealed class PropertyManagementEndpoint : ICarterModule
{
    internal const string Route = "api/admin/properties";
    internal const string Tag = "Property Management";
    internal const string Description = "Administrative endpoints for property management including CRUD and listing";
    internal const string Summary = "Property Management API";
    internal const string Name = "PropertyManagement";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(Tag)
            .WithSummary(Summary)
            .WithDescription(Description)
            .RequireAuthorization();

        // Create property
        group.MapPost("", async (
            [FromBody] CreateProperty.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            CreateProperty.Command command = new CreateProperty.Command(param);
            ErrorOr<CreateProperty.Result> result = await mediator.Send(command, cancellationToken);
            ApiResponse<CreateProperty.Result> apiResponse = result.ToApiResponseCreated("Property created successfully");

            // Add HATEOAS links for created property
            if (apiResponse is { IsSuccess: true, Data: not null })
            {
                apiResponse
                    .WithLink("self", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("update", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("delete", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("all-properties", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminAction", "property-creation");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(CreateProperty.Name)
        .WithSummary(CreateProperty.Summary)
        .WithDescription(CreateProperty.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<CreateProperty.Result>>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Property.Create);

        // List properties (paged)
        group.MapGet("", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            GetPropertyPagedList.Param param = new GetPropertyPagedList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            GetPropertyPagedList.Query query = new GetPropertyPagedList.Query(param);
            ErrorOr<PagedList<GetPropertyPagedList.Result>> result = await mediator.Send(query, cancellationToken);
            ApiResponse<List<GetPropertyPagedList.Result>> apiResponse = result.ToApiResponsePaged("Properties retrieved successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                int currentPage = (pagination.PageIndex ?? 0) + 1;
                int pageSize = pagination.PageSize ?? 10;

                apiResponse.WithLink("self", $"{Route}?page_index={currentPage}&page_size={pageSize}");

                if (apiResponse.Pagination?.HasPrevious == true)
                    apiResponse.WithLink("prev", $"{Route}?page_index={currentPage - 1}&page_size={pageSize}");

                if (apiResponse.Pagination?.HasNext == true)
                    apiResponse.WithLink("next", $"{Route}?page_index={currentPage + 1}&page_size={pageSize}");

                apiResponse.WithLink("first", $"{Route}?page_index=1&page_size={pageSize}");

                if (apiResponse.Pagination?.TotalPages > 0)
                    apiResponse.WithLink("last", $"{Route}?page_index={apiResponse.Pagination.TotalPages}&page_size={pageSize}");

                apiResponse
                    .WithLink("create-property", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminContext", "property-listing")
                    .WithMetadata("filterApplied", !string.IsNullOrEmpty(search.SearchTerm));
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetPropertyPagedList.Name)
        .WithSummary(GetPropertyPagedList.Summary)
        .WithDescription(GetPropertyPagedList.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetPropertyPagedList.Result>>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Property.List);

        // Option list for properties (lightweight items for dropdowns)
        group.MapGet("/select", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            GetPropertyOptionList.Param param = new GetPropertyOptionList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            GetPropertyOptionList.Query query = new GetPropertyOptionList.Query(param);
            ErrorOr<PagedList<GetPropertyOptionList.Result>> result = await mediator.Send(query, cancellationToken);
            ApiResponse<PagedList<GetPropertyOptionList.Result>> apiResponse = result.ToApiResponse("Option type option list retrieved successfully");

            if (apiResponse is { IsSuccess: true, Data: not null })
            {
                apiResponse
                    .WithLink("self", $"{Route}/select")
                    .WithLink("create-property", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminContext", "property-select-list");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetPropertyOptionList.Name)
        .WithSummary(GetPropertyOptionList.Summary)
        .WithDescription(GetPropertyOptionList.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetPropertyOptionList.Result>>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Property.List);

        // Get property by id
        group.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {

            GetPropertyById.Query query = new GetPropertyById.Query(id);
            ErrorOr<GetPropertyById.Result> result = await mediator.Send(query, cancellationToken);
            ApiResponse<GetPropertyById.Result> apiResponse = result.ToApiResponse("Property details retrieved successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("update", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("all-properties", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminContext", "property-details")
                    .WithMetadata("propertyId", id);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetPropertyById.Name)
        .WithSummary(GetPropertyById.Summary)
        .WithDescription(GetPropertyById.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<GetPropertyById.Result>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Property.View);

        // Update property
        group.MapPut("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateProperty.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            UpdateProperty.Command command = new UpdateProperty.Command(id, param);
            ErrorOr<UpdateProperty.Result> result = await mediator.Send(command, cancellationToken);
            ApiResponse<UpdateProperty.Result> apiResponse = result.ToApiResponse("Property updated successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("all-properties", Route)
                    .WithMetadata("adminAction", "property-update")
                    .WithMetadata("updatedAt", DateTime.UtcNow)
                    .WithMetadata("operation", "update");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(UpdateProperty.Name)
        .WithSummary(UpdateProperty.Summary)
        .WithDescription(UpdateProperty.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<UpdateProperty.Result>>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Property.Update);

        // Delete property
        group.MapDelete("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            DeleteProperty.Command command = new DeleteProperty.Command(id);
            ErrorOr<Deleted> result = await mediator.Send(command, cancellationToken);
            ApiResponse apiResponse = result.ToApiResponseDeleted("Property deleted successfully");

            apiResponse
                .WithLink("all-properties", Route)
                .WithLink("create-property", Route)
                .WithLink("all-products", "/api/admin/products")
                .WithMetadata("adminAction", "property-deletion")
                .WithMetadata("deletedAt", DateTime.UtcNow)
                .WithMetadata("deletedPropertyId", id)
                .WithMetadata("operation", "delete");

            return TypedResults.Ok(apiResponse);
        })
        .WithName(DeleteProperty.Name)
        .WithSummary(DeleteProperty.Summary)
        .WithDescription(DeleteProperty.Description)
        .WithTags(Tag)
        .Produces<ApiResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Property.Delete);
    }
}
