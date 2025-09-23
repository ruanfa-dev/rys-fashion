using Core.Catalogs;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Properties.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Catalogs.Properties.Create;
public static partial class CreateProperty
{
    public record Param : PropertyParam;
    public record Result : PropertyResult;
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

                // Check: if property with the same name exists
                var exists = await context.Properties
                    .AnyAsync(p => p.Name == param.Name, cancellationToken);

                if (exists)
                    return Property.Errors.NameAlreadyExists(param.Name);

                // Create: new property
                var property = Property.Create(
                    name: param.Name,
                    presentation: param.Presentation,
                    kind: param.Kind,
                    filterable: param.Filterable,
                    displayOn: param.DisplayOn,
                    position: param.Position
                );

                // Add: domain event
                property.AddDomainEvent(new Property.Events.Created(property.Id));

                // Save: to database
                context.Properties.Add(property);
                await context.SaveChangesAsync(cancellationToken);

                var result = property.Adapt<Result>();
                return result;
            }
            catch (Exception ex)
            {
                return Property.Errors.PropertyUnexpected(nameof(CreateProperty),ex.Message);
            }
        }
    }
}
