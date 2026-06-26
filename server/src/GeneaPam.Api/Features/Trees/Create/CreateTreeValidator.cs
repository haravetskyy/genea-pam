using FluentValidation;
using GeneaPam.Api.Features.Trees.Internal;

namespace GeneaPam.Api.Features.Trees.Create;

public sealed class CreateTreeValidator : AbstractValidator<CreateTreeCommand>
{
    public CreateTreeValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithErrorCode(TreeErrors.NameRequired.Code)
            .WithMessage(TreeErrors.NameRequired.Description);
    }
}
