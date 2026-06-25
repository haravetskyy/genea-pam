using ErrorOr;

namespace GeneaPam.Api.Features.Couples.Internal;

public static class FiliationErrors
{
    public static readonly Error NotFound = Error.NotFound(
        code: "Filiation.NotFound",
        description: "Filiation not found."
    );
}
