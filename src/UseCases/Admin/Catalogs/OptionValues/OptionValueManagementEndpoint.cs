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

using UseCases.Admin.Catalogs.OptionValues.Create;
using UseCases.Admin.Catalogs.OptionValues.Delete;
using UseCases.Admin.Catalogs.OptionValues.GetById;
using UseCases.Admin.Catalogs.OptionValues.GetPagedList;
using UseCases.Admin.Catalogs.OptionValues.GetOptionList;
using UseCases.Admin.Catalogs.OptionValues.Update;
using UseCases.Common.Extensions;
using UseCases.Common.Security.Authorization.Attributes;
using UseCases.Common.Security.Authorization.Permissions;

namespace UseCases.Admin.Catalogs.OptionValues;
public sealed class OptionValueManagementEndpoint : ICarterModule
{
    internal const string Route = "api/admin/option-values";
    internal const string Tag = "Option Value Management";
    internal const string Description = "Administrative endpoints for option value management including CRUD and listing";
    internal const string Summary = "Option Value Management API";
    internal const string Name = "OptionValueManagement";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(Tag)
            .WithSummary(Summary)
            .WithDescription(Description)
            .RequireAuthorization();

        // Create option value
        group.MapPost("", async (
            [FromBody] CreateOptionValue.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateOptionValue.Command(param);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponseCreated("Option value created successfully");

            // Add HATEOAS links for created option value
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("update", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("delete", $"{Route}/{apiResponse.Data.Id}")
                    .WithLink("all-option-values", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminAction", "optionvalue-creation");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(CreateOptionValue.Name)
        .WithSummary(CreateOptionValue.Summary)
        .WithDescription(CreateOptionValue.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<CreateOptionValue.Result>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionValue.Create);

        // List option values (paged)
        group.MapGet("", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new GetOptionValuePagedList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            var query = new GetOptionValuePagedList.Query(param);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponsePaged("Option values retrieved successfully");

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
                    .WithLink("create-option-value", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminContext", "optionvalue-listing")
                    .WithMetadata("filterApplied", !string.IsNullOrEmpty(search.SearchTerm));
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetOptionValuePagedList.Name)
        .WithSummary(GetOptionValuePagedList.Summary)
        .WithDescription(GetOptionValuePagedList.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetOptionValuePagedList.Result>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionValue.List);

        // option list for option values (lightweight items for dropdowns)
        group.MapGet("/select", async (
            [AsParameters] PagingParams pagination,
            [AsParameters] SortParams sort,
            [AsParameters] SearchParams search,
            [AsParameters] QueryFilterParams filter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var param = new GetOptionValueOptionList.Param
            {
                Paging = pagination,
                Sort = sort,
                Search = search,
                Filter = filter
            };
            var query = new GetOptionValueOptionList.Query(param);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponse("Option value option list retrieved successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/select")
                    .WithLink("create-option-value", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminContext", "optionvalue-select-list");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetOptionValueOptionList.Name)
        .WithSummary(GetOptionValueOptionList.Summary)
        .WithDescription(GetOptionValueOptionList.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<List<GetOptionValueOptionList.Result>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionValue.List);

        // Get option value by id
        group.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOptionValueById.Query(id);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponse("Option value details retrieved successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("update", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("all-option-values", Route)
                    .WithLink("all-products", "/api/admin/products")
                    .WithMetadata("adminContext", "optionvalue-details")
                    .WithMetadata("optionValueId", id);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetOptionValueById.Name)
        .WithSummary(GetOptionValueById.Summary)
        .WithDescription(GetOptionValueById.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<GetOptionValueById.Result>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionValue.Read);

        // Update option value
        group.MapPut("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateOptionValue.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateOptionValue.Command(id, param);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponse("Option value updated successfully");

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("self", $"{Route}/{id}")
                    .WithLink("delete", $"{Route}/{id}")
                    .WithLink("all-option-values", Route)
                    .WithMetadata("adminAction", "optionvalue-update")
                    .WithMetadata("updatedAt", DateTime.UtcNow)
                    .WithMetadata("operation", "update");
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(UpdateOptionValue.Name)
        .WithSummary(UpdateOptionValue.Summary)
        .WithDescription(UpdateOptionValue.Description)
        .WithTags(Tag)
        .Produces<ApiResponse<UpdateOptionValue.Result>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionValue.Update);

        // Delete option value
        group.MapDelete("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteOptionValue.Command(id);
            var result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponseDeleted("Option value deleted successfully");

            apiResponse
                .WithLink("all-option-values", Route)
                .WithLink("create-option-value", Route)
                .WithLink("all-products", "/api/admin/products")
                .WithMetadata("adminAction", "optionvalue-deletion")
                .WithMetadata("deletedAt", DateTime.UtcNow)
                .WithMetadata("deletedOptionValueId", id)
                .WithMetadata("operation", "delete");

            return TypedResults.Ok(apiResponse);
        })
        .WithName(DeleteOptionValue.Name)
        .WithSummary(DeleteOptionValue.Summary)
        .WithDescription(DeleteOptionValue.Description)
        .WithTags(Tag)
        .Produces<ApiResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequirePermission(Feature.Admin.OptionValue.Delete);
    }
}