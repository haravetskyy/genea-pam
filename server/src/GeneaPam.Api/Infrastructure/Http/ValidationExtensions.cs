using ErrorOr;
using FluentValidation;

namespace GeneaPam.Api.Infrastructure.Http;

/// <summary>
/// Shared bridge from FluentValidation to <see cref="ErrorOr{T}"/>, extracted from the
/// hand-rolled mapping that <c>RegisterValidator</c> used (ADR 0002, decision #6). Each slice
/// validator declares only rules with <c>WithErrorCode</c>/<c>WithMessage</c>; this maps a
/// failure's code + message straight into an <see cref="Error.Validation"/> so no per-validator
/// switch is needed.
/// </summary>
public static class ValidationExtensions
{
    public static async Task<ErrorOr<T>> ValidateToErrorOrAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken
    )
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.IsValid)
            return instance;

        return result
            .Errors.Select(f => Error.Validation(code: f.ErrorCode, description: f.ErrorMessage))
            .Distinct()
            .ToList();
    }
}
