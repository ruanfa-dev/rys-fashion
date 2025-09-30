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

using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
using UseCases.Common.Security.Authorization.Permissions;

using UseCases.Admin.Catalogs.Taxons.Create;
using UseCases.Admin.Catalogs.Taxons.Delete;
using UseCases.Admin.Catalogs.Taxons.Get.Id;
using UseCases.Admin.Catalogs.Taxons.Get.OptionList;
using UseCases.Admin.Catalogs.Taxons.Get.PagedList;
using UseCases.Admin.Catalogs.Taxons.Update;
using UseCases.Admin.Catalogs.Taxons.Reposition;
using UseCases.Admin.Catalogs.Taxons.Get.TreeList;

namespace UseCases.Admin.Catalogs.Taxons;

public sealed class TaxonManagementEndpoint : ICarterModule
{
    internal const string Route = "api/admin/taxons";
    internal const string Tag = "Taxon Management";
    internal const string Description = "Administrative endpoints for taxon management including CRUD and listing";
    internal const string Summary = "Taxon Management API";
    internal const string Name = "TaxonManagement";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(Tag)
            .WithSummary(Summary)
            .WithDescription(Description)
            .RequireAuthorization();

        // Create taxon
        group.MapPost("", async (
            [FromBody] CreateTaxon.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            CreateTaxon.Command command = new CreateTaxon.Command(param);
            ErrorOr<CreateTaxon.Result> result = await mediator.Send(command, cancellationToken);
            ApiResponse<CreateTaxon.Result> apiResponse = result.ToApiResponseCreated("Taxon created successfully");

            if (apiResponse is { IsSuccess: true, Data: not null })
            {
                apiResponse
                    .WithLink("self", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("update", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("delete", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("all-taxons", Route)
                    .WithMetadata("adminAction", "taxon-creation");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(CreateTaxon.Name)
        .WithSummary(CreateTaxon.Summary)
        .WithDescription(CreateTaxon.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<CreateTaxon.Result>>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxon.Create);

        // List taxons (paged)
        group.MapGet("", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            GetTaxonPagedList.Param param = new GetTaxonPagedList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            GetTaxonPagedList.Query query = new GetTaxonPagedList.Query(param);
            ErrorOr<PagedList<GetTaxonPagedList.Result>> result = await mediator.Send(query, cancellationToken);
            ApiResponse<List<GetTaxonPagedList.Result>> apiResponse = result.ToApiResponsePaged("Taxons retrieved successfully");

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
                    .WithLink("create-taxon", Route)
                    .WithMetadata("adminContext", "taxon-listing")
                    .WithMetadata("filterApplied", !string.IsNullOrEmpty(search.SearchTerm));
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetTaxonPagedList.Name)
        .WithSummary(GetTaxonPagedList.Summary)
        .WithDescription(GetTaxonPagedList.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetTaxonPagedList.Result>>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxon.List);

        // Option list for taxons (lightweight items)
        group.MapGet("/select", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            GetTaxonOptionList.Param param = new GetTaxonOptionList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            GetTaxonOptionList.Query query = new GetTaxonOptionList.Query(param);
            ErrorOr<PagedList<GetTaxonOptionList.Result>> result = await mediator.Send(query, cancellationToken);
            ApiResponse<PagedList<GetTaxonOptionList.Result>> apiResponse = result.ToApiResponse("Taxon option list retrieved successfully");

            if (apiResponse is { IsSuccess: true, Data: not null })
            {
                apiResponse
                    .WithLink("self", $"{Route}/select")
                    .WithLink("create-taxon", Route)
                    .WithMetadata("adminContext", "taxon-select-list");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetTaxonOptionList.Name)
        .WithSummary(GetTaxonOptionList.Summary)
        .WithDescription(GetTaxonOptionList.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetTaxonOptionList.Result>>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxon.List);

        // Get taxon by id
        group.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            GetTaxonById.Query query = new GetTaxonById.Query(id);
            ErrorOr<GetTaxonById.Result> result = await mediator.Send(query, cancellationToken);
            ApiResponse<GetTaxonById.Result> apiResponse = result.ToApiResponse("Taxon details retrieved successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("update", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("all-taxons", Route)
                    .WithMetadata("adminContext", "taxon-details")
                    .WithMetadata("taxonId", id);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetTaxonById.Name)
        .WithSummary(GetTaxonById.Summary)
        .WithDescription(GetTaxonById.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<GetTaxonById.Result>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxon.View);

        // Update taxon
        group.MapPut("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateTaxon.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            UpdateTaxon.Command command = new UpdateTaxon.Command(id, param);
            ErrorOr<UpdateTaxon.Result> result = await mediator.Send(command, cancellationToken);
            ApiResponse<UpdateTaxon.Result> apiResponse = result.ToApiResponse("Taxon updated successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("all-taxons", Route)
                    .WithMetadata("adminAction", "taxon-update")
                    .WithMetadata("updatedAt", DateTime.UtcNow)
                    .WithMetadata("operation", "update");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(UpdateTaxon.Name)
        .WithSummary(UpdateTaxon.Summary)
        .WithDescription(UpdateTaxon.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<UpdateTaxon.Result>>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxon.Update);

        // Reposition taxon
        group.MapPost("/reposition", async (
            [FromBody] RepositionTaxon.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            RepositionTaxon.Command command = new RepositionTaxon.Command(param);
            ErrorOr<RepositionTaxon.Result> result = await mediator.Send(command, cancellationToken);
            ApiResponse<RepositionTaxon.Result> apiResponse = result.ToApiResponse("Taxon repositioned successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("all-taxons", Route)
                    .WithMetadata("adminAction", "taxon-reposition")
                    .WithMetadata("updatedAt", DateTime.UtcNow)
                    .WithMetadata("operation", "reposition");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(RepositionTaxon.Name)
        .WithSummary(RepositionTaxon.Summary)
        .WithDescription(RepositionTaxon.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<RepositionTaxon.Result>>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxon.Update);

        // Delete taxon
        group.MapDelete("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            DeleteTaxon.Command command = new DeleteTaxon.Command(id);
            ErrorOr<Deleted> result = await mediator.Send(command, cancellationToken);
            ApiResponse<Deleted> apiResponse = result.ToApiResponse("Taxon deleted successfully");

            apiResponse
                .WithLink("all-taxons", Route)
                .WithLink("create-taxon", Route)
                .WithMetadata("adminAction", "taxon-deletion")
                .WithMetadata("deletedAt", DateTime.UtcNow)
                .WithMetadata("deletedTaxonId", id)
                .WithMetadata("operation", "delete");

            return TypedResults.Ok(apiResponse);
        })
        .WithName(DeleteTaxon.Name)
        .WithSummary(DeleteTaxon.Summary)
        .WithDescription(DeleteTaxon.Description)
        .WithTags(Tag)
        .Produces<ApiResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxon.Delete);

        // Taxon tree (hierarchical)
        group.MapGet("/tree", async (
            [FromQuery(Name = "taxonomy_id")] Guid? taxonomyId,
            [FromQuery(Name = "store_id")] Guid? storeId,
            [FromQuery(Name = "include_leaves_only")] bool? includeLeavesOnly,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            // Default to false so that tree endpoint returns full hierarchy unless explicitly asked for leaves-only
            GetTaxonTree.Param param = new GetTaxonTree.Param(taxonomyId, storeId, includeLeavesOnly ?? false);

            GetTaxonTree.Query query = new GetTaxonTree.Query(param);
            ErrorOr<List<GetTaxonTree.Result>> result = await mediator.Send(query, cancellationToken);
            ApiResponse<List<GetTaxonTree.Result>> apiResponse = result.ToApiResponse("Taxon tree retrieved successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                List<string> qs = new List<string>();
                if (taxonomyId.HasValue) qs.Add($"taxonomy_id={taxonomyId}");
                if (storeId.HasValue) qs.Add($"store_id={storeId}");
                if (includeLeavesOnly.HasValue) qs.Add($"include_leaves_only={includeLeavesOnly.Value.ToString().ToLowerInvariant()}");
                string q = qs.Count > 0 ? "?" + string.Join("&", qs) : string.Empty;

                apiResponse
                    .WithLink("self", $"{Route}/tree{q}")
                    .WithLink("create-taxon", Route)
                    .WithMetadata("adminContext", "taxon-tree");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetTaxonTree.Name)
        .WithSummary(GetTaxonTree.Summary)
        .WithDescription(GetTaxonTree.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetTaxonTree.Result>>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.Taxon.List);
    }
}
