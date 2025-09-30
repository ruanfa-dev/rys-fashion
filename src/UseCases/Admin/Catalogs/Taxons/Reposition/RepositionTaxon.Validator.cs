using Core.Catalog.Taxonomies;

using FluentValidation;

namespace UseCases.Admin.Catalogs.Taxons.Reposition;

public partial class RepositionTaxon
{
    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {
            RuleFor(x => x.Id)
                .NotEqual(Guid.Empty)
                .WithErrorCode(Taxon.Errors.InvalidId.Code)
                .WithMessage(Taxon.Errors.InvalidId.Description);

            RuleFor(x => x.Index)
                .InclusiveBetween(Taxon.Constraints.PositionMin, Taxon.Constraints.PositionMax)
                .WithErrorCode(Taxon.Errors.InvalidPosition.Code)
                .WithMessage(Taxon.Errors.InvalidPosition.Description);

            // Prevent self-parenting at validation layer (handler/domain will also guard)
            RuleFor(x => x.ParentId)
                .NotEqual(x => x.Id)
                .When(x => x.ParentId.HasValue)
                .WithErrorCode(Taxon.Errors.SelfParent.Code)
                .WithMessage(Taxon.Errors.SelfParent.Description);
        }
    }
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(m => m.Param)
                .SetValidator(new ParamValidator());
        }
    }
}
