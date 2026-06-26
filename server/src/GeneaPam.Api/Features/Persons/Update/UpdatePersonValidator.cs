using FluentValidation;
using GeneaPam.Api.Features.Persons.Internal;

namespace GeneaPam.Api.Features.Persons.Update;

public sealed class UpdatePersonValidator : AbstractValidator<UpdatePersonCommand>
{
    public UpdatePersonValidator()
    {
        RuleFor(c => c.FirstName)
            .NotEmpty()
            .WithErrorCode(PersonErrors.FirstNameRequired.Code)
            .WithMessage(PersonErrors.FirstNameRequired.Description);
    }
}
