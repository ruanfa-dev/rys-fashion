using Core.Catalog.Taxonomies;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Extensions.Text;
using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Taxonomies.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxonomies.Create;
public static partial class CreateTaxonomy
{
    public sealed record Param : TaxonomyParam;
    public sealed record Result : TaxonomyResult.ListItem;
    public sealed record Command(Param Param) : ICommand<Result>;
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
                // Load: param
                Param param = request.Param;
                string parameterize = param.Name.Parameterize();

                // Check: uniqueness of name
                bool exists = await context.Set<Taxonomy>()
                    .AnyAsync(t => t.Name == parameterize && (param.StoreId == null || t.StoreId == param.StoreId), cancellationToken);
                if (exists)
                    return Taxonomy.Errors.NameAlreadyExists(parameterize);

                // Create: new entity
                Guid storeId = param.StoreId ?? Guid.Empty;
                ErrorOr<Taxonomy> createResult = Taxonomy.Create(parameterize, storeId: storeId, position: param.Position);
                if (createResult.IsError)
                    return createResult.Errors;

                // Persist: new entity
                context.Set<Taxonomy>().Add(createResult.Value);
                await context.SaveChangesAsync(cancellationToken);

                Result result = createResult.Value.Adapt<Result>();
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
