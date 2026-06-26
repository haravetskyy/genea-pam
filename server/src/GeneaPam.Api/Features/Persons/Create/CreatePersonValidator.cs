using FluentValidation;
using GeneaPam.Api.Features.Persons.Internal;

namespace GeneaPam.Api.Features.Persons.Create;

public sealed class CreatePersonValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonValidator()
    {
        RuleFor(c => c.FirstName)
            .NotEmpty()
            .WithErrorCode(PersonErrors.FirstNameRequired.Code)
            .WithMessage(PersonErrors.FirstNameRequired.Description);
    }
}
