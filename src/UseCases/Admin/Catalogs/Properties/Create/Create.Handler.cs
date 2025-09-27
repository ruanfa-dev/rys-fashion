using Core.Catalog.Properties;
using Core.Commons.Extensions;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Properties.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Properties.Create;
public static partial class CreateProperty
{
    public sealed record Param : PropertyParam;
    public sealed record Result : PropertyResult.ListItem;
    public sealed record Command(Param Param) : ICommand<Result>;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param).SetValidator(new PropertyParamValidator());
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

                // Check: uniqueness of name
                var name = param.Name.ComputeFilterParam();
                var exists = await context.Set<Property>()
                    .AnyAsync(p => p.Name == name, cancellationToken);

                if (exists)
                    return Property.Errors.NameAlreadyExists(name);

                // Create: new entity
                var createResult = Property.Create(
                    name: name,
                    presentation: param.Presentation,
                    kind: param.Kind,
                    filterable: param.Filterable,
                    displayOn: param.DisplayOn,
                    position: param.Position
                );

                if (createResult.IsError)
                    return createResult.Errors;

                // Save: to database
                context.Set<Property>().Add(createResult.Value);
                await context.SaveChangesAsync(cancellationToken);

                var result = createResult.Value.Adapt<Result>();
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, messageTemplate: "An error occurred while creating a new property with name {PropertyName}", request.Param.Name);
                return Property.Errors.UnexpectedError(nameof(CreateProperty), ex);
            }
        }
    }
}
