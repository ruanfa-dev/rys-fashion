using Core.Catalog.Properties;
using Core.Commons.Extensions;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Extensions.Text;
using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Properties.Commons;
using UseCases.Admin.Catalogs.Properties.Create;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Properties.Update;
public static partial class UpdateProperty
{
    public record Param : PropertyParam;
    public record Result : PropertyResult.ListItem;
    public sealed record Command(Guid Id, Param Param) : ICommand<Result>;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty()
                .WithErrorCode(Property.Errors.IdRequired.Code)
                .WithMessage(Property.Errors.IdRequired.Description);
            RuleFor(x => x.Param)
                .SetValidator(new PropertyParamValidator());
        }
    }
    public sealed class Handler(
        IUnitOfWork unitOfWork
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var dbContext = unitOfWork.Context;
                var entity = await dbContext.Set<Property>()
                    .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
                if (entity is null)
                    return Property.Errors.NotFound(request.Id);

                var param = request.Param;

                // Check: for name uniqueness
                var name = param.Name.Parameterize();
                var nameExists = await dbContext.Set<Property>()
                    .AnyAsync(p => p.Id != request.Id && p.Name == name, cancellationToken);
                if (nameExists)
                    return Property.Errors.NameAlreadyExists(name);

                // Update: entity
                var updateResult = entity.Update(
                    name: name,
                    presentation: param.Presentation,
                    kind: param.Kind,
                    filterable: param.Filterable,
                    displayOn: param.DisplayOn,
                    position: param.Position,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: param.PrivateMetadata
                );
                if (updateResult.IsError)
                    return updateResult.Errors;

                // Save: changes
                dbContext.Set<Property>().Update(updateResult.Value);
                var result = updateResult.Value.Adapt<Result>();
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while updating property {PropertyId}", request.Id);
                return Property.Errors.UnexpectedError(nameof(CreateProperty), ex);
            }
        }
    }
}
