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

using UseCases.Admin.Catalogs.OptionTypes.Create;
using UseCases.Admin.Catalogs.OptionTypes.Delete;
using UseCases.Admin.Catalogs.OptionTypes.GetById;
using UseCases.Admin.Catalogs.OptionTypes.GetPagedList;
using UseCases.Admin.Catalogs.OptionTypes.GetOptionList;
using UseCases.Admin.Catalogs.OptionTypes.Update;
using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
using UseCases.Common.Security.Authorization.Permissions;

namespace UseCases.Admin.Catalogs.OptionTypes;
public sealed class OptionTypeManagementEndpoint : ICarterModule
{
    internal const string Route = "api/admin/option-types";
    internal const string Tag = "Option Type Management";
    internal const string Description = "Administrative endpoints for option type management including CRUD and listing";
    internal const string Summary = "Option Type Management API";
    internal const string Name = "OptionTypeManagement";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
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
            var command = new CreateOptionType.Command(param);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponseCreated("Option type created successfully");

            // Add HATEOAS links for created option type
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("update", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("delete", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("all-option-types", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminAction", "optiontype-creation");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(CreateOptionType.Name)
        .WithSummary(CreateOptionType.Summary)
        .WithDescription(CreateOptionType.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<CreateOptionType.Result>>(StatusCodes.Status200OK)
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
            var param = new GetOptionTypePagedList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            var query = new GetOptionTypePagedList.Query(param);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponsePaged("Option types retrieved successfully");

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
                    .WithLink("create-option-type", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminContext", "optiontype-listing")
                    .WithMetadata("filterApplied", !string.IsNullOrEmpty(search.SearchTerm));
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetOptionTypePagedList.Name)
        .WithSummary(GetOptionTypePagedList.Summary)
        .WithDescription(GetOptionTypePagedList.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetOptionTypePagedList.Result>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionType.List);

        // Option list for option types (lightweight items for dropdowns)
        group.MapGet("/select", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new GetOptionTypeOptionList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            var query = new GetOptionTypeOptionList.Query(param);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponse("Option type option list retrieved successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/select")
                    .WithLink("create-option-type", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminContext", "optiontype-select-list");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetOptionTypeOptionList.Name)
        .WithSummary(GetOptionTypeOptionList.Summary)
        .WithDescription(GetOptionTypeOptionList.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetOptionTypeOptionList.Result>>>(StatusCodes.Status200OK)
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

            var query = new GetOptionTypeById.Query(id);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponse("Option type details retrieved successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("update", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("all-option-types", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminContext", "optiontype-details")
                    .WithMetadata("optionTypeId", id);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetOptionTypeById.Name)
        .WithSummary(GetOptionTypeById.Summary)
        .WithDescription(GetOptionTypeById.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<GetOptionTypeById.Result>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionType.Read);

        // Update option type
        group.MapPut("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateOptionType.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateOptionType.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponse("Option type updated successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("all-option-types", Route)
                    .WithMetadata("adminAction", "optiontype-update")
                    .WithMetadata("updatedAt", DateTime.UtcNow)
                    .WithMetadata("operation", "update");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(UpdateOptionType.Name)
        .WithSummary(UpdateOptionType.Summary)
        .WithDescription(UpdateOptionType.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<UpdateOptionType.Result>>(StatusCodes.Status200OK)
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
            var command = new DeleteOptionType.Command(id);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponseDeleted("Option type deleted successfully");

            apiResponse
                .WithLink("all-option-types", Route)
                .WithLink("create-option-type", Route)
                .WithLink("all-products", "/api/admin/products")
                .WithMetadata("adminAction", "optiontype-deletion")
                .WithMetadata("deletedAt", DateTime.UtcNow)
                .WithMetadata("deletedOptionTypeId", id)
                .WithMetadata("operation", "delete");

            return TypedResults.Ok(apiResponse);
        })
        .WithName(DeleteOptionType.Name)
        .WithSummary(DeleteOptionType.Summary)
        .WithDescription(DeleteOptionType.Description)
        .WithTags(Tag)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionType.Delete);
    }
}