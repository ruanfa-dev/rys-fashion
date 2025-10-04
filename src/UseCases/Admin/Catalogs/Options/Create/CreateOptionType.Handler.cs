using Core.Catalog.Options;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Extensions.Text;
using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Options.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Options.Create;
public static partial class CreateOptionType
{
    public sealed record Param : OptionTypeParam;
    public sealed record Result : OptionTypeResult.ListItem;
    public sealed record Command(Param Param) : ICommand<Result>;
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Param).SetValidator(new OptionTypeParamValidator());
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
                Param param = request.Param;

                // Check: uniqueness of name
                string name = param.Name.Parameterize();
                bool exists = await context.Set<OptionType>()
                    .AnyAsync(p => p.Name == name, cancellationToken);

                if (exists)
                    return OptionType.Errors.NameAlreadyExists(name);

                // Create: new entity
                ErrorOr<OptionType> createResult = OptionType.Create(
                    name: name,
                    presentation: param.Presentation,
                    filterable: param.Filterable,
                    position: param.Position,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: param.PrivateMetadata
                );

                if (createResult.IsError)
                    return createResult.Errors;

                // Save: to database
                context.Set<OptionType>().Add(createResult.Value);
                await context.SaveChangesAsync(cancellationToken);

                Result result = createResult.Value.Adapt<Result>();
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, messageTemplate: "An error occurred while creating a new property with name {OptionTypeName}", request.Param.Name);
                return OptionType.Errors.UnexpectedError(nameof(Options.Create.CreateOptionType), ex);
            }
        }
    }
}
