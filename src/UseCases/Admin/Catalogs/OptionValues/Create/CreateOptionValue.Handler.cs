using Core.Catalogs;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.OptionValues.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.OptionValues.Create;
public static partial class CreateOptionValue
{
    public record Param : OptionValueParam;
    public record Result : OptionValueResult;
    public sealed record Command(Param Param) : ICommand<Result>;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param)
                .SetValidator(new OptionValueParamValidator());
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

                // Check: option type existing
                var optionType = await context.Set<OptionType>()
                    .Include(ot => ot.OptionValues)
                    .FirstOrDefaultAsync(ot => ot.Id == param.OptionTypeId, cancellationToken);

                if (optionType is null)
                    return OptionType.Errors.NotFound(param.OptionTypeId);

                // Check: if option value with the same name exists within the option type
                var exists = optionType.OptionValues
                    .Any(p => p.Name == param.Name);
                if (exists)
                    return OptionValue.Errors.DuplicateForOptionType(optionValue: param.Name, optionType: optionType.Name);

                // Create: new option value
                var property = OptionValue.Create(
                    optionTypeId: param.OptionTypeId,
                    name: param.Name,
                    presentation: param.Presentation,
                    position: param.Position
                );

                // Add: domain event
                property.AddDomainEvent(new OptionValue.Events.Created(property.Id));

                // Save: to database
                context.Set<OptionValue>().Add(property);
                await context.SaveChangesAsync(cancellationToken);

                var result = property.Adapt<Result>();
                return result;
            }
            catch (Exception ex)
            {
                return OptionValue.Errors.OptionValueUnexpected(nameof(CreateOptionValue), ex.Message);
            }
        }
    }
}
