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

using UseCases.Admin.Catalogs.Options.Create;
using UseCases.Admin.Catalogs.Options.Delete;
using UseCases.Admin.Catalogs.Options.Get.Id;
using UseCases.Admin.Catalogs.Options.Get.OptionList;
using UseCases.Admin.Catalogs.Options.Get.PagedList;
using UseCases.Admin.Catalogs.Options.Update;
using UseCases.Admin.Catalogs.Properties.Get.PagedList;
using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
using UseCases.Common.Security.Authorization.Permissions;

namespace UseCases.Admin.Catalogs.Options;

public sealed class OptionTypeManagementEndpoint : ICarterModule
{
    private const string Route = "api/admin/option-types";
    private const string Tag = "Option Type Management";
    private const string Description = "Administrative endpoints for option type management including CRUD and listing";
    private const string Summary = "Option Type Management API";
    private const string Name = "OptionTypeManagement";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(Tag)
            .WithSummary(Summary)
            .WithDescription(Description)
            .RequireAuthorization();

        // Create option type
        group.MapPost("", async (
            [FromBody] CreateOptionType.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            CreateOptionType.Command command = new(param);
            ErrorOr<CreateOptionType.Result> result = await mediator.Send(command, cancellationToken);
            ApiResponse<CreateOptionType.Result> apiResponse = result.ToApiResponseCreated("Option type created successfully");

            if (apiResponse is { IsSuccess: true, Data: not null })
            {
                apiResponse
                    .WithLink("self", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("update", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("delete", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("all-option-types", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminAction", "option-type-creation");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(CreateOptionType.Name)
        .WithSummary(CreateOptionType.Summary)
        .WithDescription(CreateOptionType.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<CreateOptionType.Result>>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionType.Create);

        // List option types (paged)
        group.MapGet("", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            GetOptionTypePagedList.Param param = new GetOptionTypePagedList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            GetOptionTypePagedList.Query query = new GetOptionTypePagedList.Query(param);
            ErrorOr<PagedList<GetOptionTypePagedList.Result>> result = await mediator.Send(query, cancellationToken);
            ApiResponse<List<GetOptionTypePagedList.Result>> apiResponse = result.ToApiResponsePaged("Option types retrieved successfully");

            if (apiResponse is { IsSuccess: true, Data: not null })
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
                    .WithLink("create-option-type", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminContext", "option-type-listing")
                    .WithMetadata("filterApplied", !string.IsNullOrEmpty(search.SearchTerm));
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetOptionTypePagedList.Name)
        .WithSummary(GetOptionTypePagedList.Summary)
        .WithDescription(GetOptionTypePagedList.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetOptionTypePagedList.Result>>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionType.List);

        // Option type select list (lightweight for dropdowns)
        group.MapGet("/select", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            GetOptionTypeOptionList.Param param = new GetOptionTypeOptionList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            GetOptionTypeOptionList.Query query = new GetOptionTypeOptionList.Query(param);
            ErrorOr<PagedList<GetOptionTypeOptionList.Result>> result = await mediator.Send(query, cancellationToken);
            ApiResponse<PagedList<GetOptionTypeOptionList.Result>> apiResponse = result.ToApiResponse("Option type select list retrieved successfully");

            if (apiResponse is { IsSuccess: true, Data: not null })
            {
                apiResponse
                    .WithLink("self", $"{Route}/select")
                    .WithLink("create-option-type", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminContext", "option-type-select-list");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetOptionTypeOptionList.Name)
        .WithSummary(GetOptionTypeOptionList.Summary)
        .WithDescription(GetOptionTypeOptionList.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetOptionTypeOptionList.Result>>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionType.List);

        // Get option type by id
        group.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            GetOptionTypeById.Query query = new GetOptionTypeById.Query(id);
            ErrorOr<GetOptionTypeById.Result> result = await mediator.Send(query, cancellationToken);
            ApiResponse<GetOptionTypeById.Result> apiResponse = result.ToApiResponse("Option type details retrieved successfully");

            if (apiResponse is { IsSuccess: true, Data: not null })
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("update", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("all-option-types", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminContext", "option-type-details")
                    .WithMetadata("optionTypeId", id);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetOptionTypeById.Name)
        .WithSummary(GetOptionTypeById.Summary)
        .WithDescription(GetOptionTypeById.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<GetOptionTypeById.Result>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionType.View);

        // Update option type
        group.MapPut("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateOptionType.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            UpdateOptionType.Command command = new UpdateOptionType.Command(id, param);
            ErrorOr<UpdateOptionType.Result> result = await mediator.Send(command, cancellationToken);
            ApiResponse<UpdateOptionType.Result> apiResponse = result.ToApiResponse("Option type updated successfully");

            if (apiResponse is { IsSuccess: true, Data: not null })
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("all-option-types", Route)
                    .WithMetadata("adminAction", "option-type-update")
                    .WithMetadata("updatedAt", DateTime.UtcNow)
                    .WithMetadata("operation", "update");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(UpdateOptionType.Name)
        .WithSummary(UpdateOptionType.Summary)
        .WithDescription(UpdateOptionType.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<UpdateOptionType.Result>>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionType.Update);

        // Delete option type
        group.MapDelete("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            DeleteOptionType.Command command = new DeleteOptionType.Command(id);
            ErrorOr<Deleted> result = await mediator.Send(command, cancellationToken);
            ApiResponse apiResponse = result.ToApiResponseDeleted("Option type deleted successfully");

            apiResponse
                .WithLink("all-option-types", Route)
                .WithLink("create-option-type", Route)
                .WithLink("all-products", "/api/admin/products")
                .WithMetadata("adminAction", "option-type-deletion")
                .WithMetadata("deletedAt", DateTime.UtcNow)
                .WithMetadata("deletedOptionTypeId", id)
                .WithMetadata("operation", "delete");

            return TypedResults.Ok(apiResponse);
        })
        .WithName(DeleteOptionType.Name)
        .WithSummary(DeleteOptionType.Summary)
        .WithDescription(DeleteOptionType.Description)
        .WithTags(Tag)
        .Produces<ApiResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionType.Delete);
    }
}