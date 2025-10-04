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

namespace UseCases.Admin.Catalogs.Options.Update;
public static partial class UpdateOptionType
{
    public record Param : OptionTypeParam;
    public record Result : OptionTypeResult.ListItem;
    public sealed record Command(Guid Id, Param Param) : ICommand<Result>;
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty()
                .WithErrorCode(OptionType.Errors.IdRequired.Code)
                .WithMessage(OptionType.Errors.IdRequired.Description);
            RuleFor(x => x.Param)
                .SetValidator(new OptionTypeParamValidator());
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
                IApplicationDbContext dbContext = unitOfWork.Context;
                OptionType? entity = await dbContext.Set<OptionType>()
                    .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
                if (entity is null)
                    return OptionType.Errors.NotFound(request.Id);

                Param param = request.Param;

                // Check: for name uniqueness
                string name = param.Name.Parameterize();
                bool nameExists = await dbContext.Set<OptionType>()
                    .AnyAsync(p => p.Id != request.Id && p.Name == name, cancellationToken);
                if (nameExists)
                    return OptionType.Errors.NameAlreadyExists(name);

                // Update: entity
                ErrorOr<OptionType> updateResult = entity.Update(
                    name: name,
                    presentation: param.Presentation,
                    filterable: param.Filterable,
                    position: param.Position,
                    publicMetadata: param.PublicMetadata,
                    privateMetadata: param.PrivateMetadata
                );
                if (updateResult.IsError)
                    return updateResult.Errors;

                // Save: changes
                dbContext.Set<OptionType>().Update(updateResult.Value);
                Result result = updateResult.Value.Adapt<Result>();
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while updating property {OptionTypeId}", request.Id);
                return OptionType.Errors.UnexpectedError(nameof(UpdateOptionType), ex);
            }
        }
    }
}
