using ErrorOr;

namespace GeneaPam.Api.Features.Couples.Internal;

public static class CoupleErrors
{
    public static readonly Error NotFound = Error.NotFound(
        code: "Couple.NotFound",
        description: "Couple not found."
    );

    public static readonly Error SamePersonBothSides = Error.Validation(
        code: "Couple.SamePersonBothSides",
        description: "A Person cannot be both person_a and person_b in the same Couple."
    );
}
