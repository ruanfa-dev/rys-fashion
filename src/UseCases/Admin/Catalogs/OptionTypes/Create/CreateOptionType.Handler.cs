using Core.Catalogs;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.OptionTypes.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.OptionTypes.Create;
public static partial class CreateOptionType
{
    public record Param : OptionTypeParam;
    public record Result : OptionTypeResult;
    public sealed record Command(Param Param) : ICommand<Result>;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
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
                var param = request.Param;

                // Check: if property with the same name exists
                var exists = await context.Properties
                    .AnyAsync(p => p.Name == param.Name, cancellationToken);

                if (exists)
                    return OptionType.Errors.DuplicateName(param.Name);

                // Create: new property
                var property = OptionType.Create(
                    name: param.Name,
                    presentation: param.Presentation,
                    filterable: param.Filterable,
                    position: param.Position ?? OptionType.Constants.DefaultPosition
                );

                // Add: domain event
                property.AddDomainEvent(new OptionType.Events.Created(property.Id));

                // Save: to database
                context.Set<OptionType>().Add(property);
                await context.SaveChangesAsync(cancellationToken);

                var result = property.Adapt<Result>();
                return result;
            }
            catch (Exception ex)
            {
                return OptionType.Errors.OptionTypeUnexpected(nameof(CreateOptionType), ex.Message);
            }
        }
    }
}
