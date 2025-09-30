using Core.Catalog.Taxonomies;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Taxonomies.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxonomies.Create;
public static partial class CreateTaxonomy
{
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param).SetValidator(new TaxonomyParamValidator());
        }
    }

    public sealed class Handler(
        IApplicationDbContext context
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;

                var trimmed = param.Name?.Trim() ?? string.Empty;

                var exists = await context.Set<Taxonomy>()
                    .AnyAsync(t => t.Name == trimmed && (param.StoreId == null || t.StoreId == param.StoreId), cancellationToken);
                if (exists)
                    return Taxonomy.Errors.NameAlreadyExists(trimmed);

                var storeId = param.StoreId ?? Guid.Empty;
                var createResult = Taxonomy.Create(trimmed, storeId: storeId, position: param.Position);
                if (createResult.IsError)
                    return createResult.Errors;

                context.Set<Taxonomy>().Add(createResult.Value);
                await context.SaveChangesAsync(cancellationToken);

                var result = createResult.Value.Adapt<Result>();
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred creating taxonomy {Name}", request.Param.Name);
                return Taxonomy.Errors.UnexpectedError(nameof(CreateTaxonomy), ex);
            }
        }
    }
}
