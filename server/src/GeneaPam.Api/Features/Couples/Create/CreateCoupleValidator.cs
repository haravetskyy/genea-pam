using FluentValidation;
using GeneaPam.Api.Features.Couples.Internal;

namespace GeneaPam.Api.Features.Couples.Create;

public sealed class CreateCoupleValidator : AbstractValidator<CreateCoupleCommand>
{
    public CreateCoupleValidator()
    {
        RuleFor(c => c.PersonBId)
            .NotEqual(c => c.PersonAId)
            .WithErrorCode(CoupleErrors.SamePersonBothSides.Code)
            .WithMessage(CoupleErrors.SamePersonBothSides.Description);
    }
}
