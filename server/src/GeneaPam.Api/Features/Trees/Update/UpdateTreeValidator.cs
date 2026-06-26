using FluentValidation;
using GeneaPam.Api.Features.Trees.Internal;

namespace GeneaPam.Api.Features.Trees.Update;

public sealed class UpdateTreeValidator : AbstractValidator<UpdateTreeCommand>
{
    public UpdateTreeValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithErrorCode(TreeErrors.NameRequired.Code)
            .WithMessage(TreeErrors.NameRequired.Description);
    }
}
