using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;
using SharedKernel.Models.Filter;
using SharedKernel.Models.Paging;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

using UseCases.Admin.Catalogs.Taxonomies.Create;
using UseCases.Admin.Catalogs.Taxonomies.Delete;
using UseCases.Admin.Catalogs.Taxonomies.Get.Id;
using UseCases.Admin.Catalogs.Taxonomies.Get.OptionList;
using UseCases.Admin.Catalogs.Taxonomies.Get.PagedList;
using UseCases.Admin.Catalogs.Taxonomies.Update;
using UseCases.Admin.Catalogs.Taxonomies.Commons;
using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
using UseCases.Common.Security.Authorization.Permissions;

namespace UseCases.Admin.Catalogs.Taxonomies;

public sealed class TaxonomyManagementEndpoint : ICarterModule
{
    internal const string Route = "api/admin/taxonomies";
    internal const string Tag = "Taxonomy Management";
    internal const string Description = "Administrative endpoints for taxonomy management including CRUD and listing";
    internal const string Summary = "Taxonomy Management API";
    internal const string Name = "TaxonomyManagement";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(Tag)
            .WithSummary(Summary)
            .WithDescription(Description)
            .RequireAuthorization();

        // Create taxonomy
        group.MapPost("", async (
            [FromBody] CreateTaxonomy.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateTaxonomy.Command(param);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponseCreated("Taxonomy created successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("update", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("delete", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("all-taxonomies", Route)
                    .WithMetadata("adminAction", "taxonomy-creation");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(CreateTaxonomy.Name)
        .WithSummary(CreateTaxonomy.Summary)
        .WithDescription(CreateTaxonomy.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<CreateTaxonomy.Result>>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxonomy.Create);

        // List taxonomies (paged)
        group.MapGet("", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new GetTaxonomyPagedList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            var query = new GetTaxonomyPagedList.Query(param);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponsePaged("Taxonomies retrieved successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var currentPage = (pagination.PageIndex ?? 0) + 1;
                var pageSize = pagination.PageSize ?? 10;

                apiResponse.WithLink("self", $"{Route}?page_index={currentPage}&page_size={pageSize}");

                if (apiResponse.Pagination?.HasPrevious == true)
                    apiResponse.WithLink("prev", $"{Route}?page_index={currentPage - 1}&page_size={pageSize}");

                if (apiResponse.Pagination?.HasNext == true)
                    apiResponse.WithLink("next", $"{Route}?page_index={currentPage + 1}&page_size={pageSize}");

                apiResponse.WithLink("first", $"{Route}?page_index=1&page_size={pageSize}");

                if (apiResponse.Pagination?.TotalPages > 0)
                    apiResponse.WithLink("last", $"{Route}?page_index={apiResponse.Pagination.TotalPages}&page_size={pageSize}");

                apiResponse
                    .WithLink("create-taxonomy", Route)
                    .WithMetadata("adminContext", "taxonomy-listing")
                    .WithMetadata("filterApplied", !string.IsNullOrEmpty(search.SearchTerm));
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetTaxonomyPagedList.Name)
        .WithSummary(GetTaxonomyPagedList.Summary)
        .WithDescription(GetTaxonomyPagedList.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetTaxonomyPagedList.Result>>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxonomy.List);

        // Option list for taxonomies (lightweight items)
        group.MapGet("/select", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new GetTaxonomyOptionList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            var query = new GetTaxonomyOptionList.Query(param);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponse("Taxonomy option list retrieved successfully");

            if (apiResponse is { IsSuccess: true, Data: not null })
            {
                apiResponse
                    .WithLink("self", $"{Route}/select")
                    .WithLink("create-taxonomy", Route)
                    .WithMetadata("adminContext", "taxonomy-select-list");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetTaxonomyOptionList.Name)
        .WithSummary(GetTaxonomyOptionList.Summary)
        .WithDescription(GetTaxonomyOptionList.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetTaxonomyOptionList.Result>>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxonomy.List);

        // Get taxonomy by id
        group.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTaxonomyById.Query(id);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponse("Taxonomy details retrieved successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("update", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("all-taxonomies", Route)
                    .WithMetadata("adminContext", "taxonomy-details")
                    .WithMetadata("taxonomyId", id);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetTaxonomyById.Name)
        .WithSummary(GetTaxonomyById.Summary)
        .WithDescription(GetTaxonomyById.Description)
        .WithTags(Tag)
    .Produces<ApiResponse<TaxonomyResult.Details>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxonomy.View);

        // Update taxonomy
        group.MapPut("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateTaxonomy.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateTaxonomy.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponse("Taxonomy updated successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("all-taxonomies", Route)
                    .WithMetadata("adminAction", "taxonomy-update")
                    .WithMetadata("updatedAt", DateTime.UtcNow)
                    .WithMetadata("operation", "update");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(UpdateTaxonomy.Name)
        .WithSummary(UpdateTaxonomy.Summary)
        .WithDescription(UpdateTaxonomy.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<UpdateTaxonomy.Result>>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxonomy.Update);

        // Delete taxonomy
        group.MapDelete("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteTaxonomy.Command(id);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponse("Taxonomy deleted successfully");

            apiResponse
                .WithLink("all-taxonomies", Route)
                .WithLink("create-taxonomy", Route)
                .WithMetadata("adminAction", "taxonomy-deletion")
                .WithMetadata("deletedAt", DateTime.UtcNow)
                .WithMetadata("deletedTaxonomyId", id)
                .WithMetadata("operation", "delete");

            return TypedResults.Ok(apiResponse);
        })
        .WithName(DeleteTaxonomy.Name)
        .WithSummary(DeleteTaxonomy.Summary)
        .WithDescription(DeleteTaxonomy.Description)
        .WithTags(Tag)
    .Produces<ApiResponse<DeleteTaxonomy.Deleted>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxonomy.Delete);
    }
}
