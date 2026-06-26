using ErrorOr;

namespace GeneaPam.Api.Features.Couples.Internal;

public static class FiliationErrors
{
    public static readonly Error NotFound = Error.NotFound(
        code: "Filiation.NotFound",
        description: "Filiation not found."
    );

    public static readonly Error ParentageTypeInvalid = Error.Validation(
        code: "Filiation.ParentageTypeInvalid",
        description: "Parentage type must be one of: Biological, Adoptive, Step, Foster."
    );

    public static readonly Error SelfParent = Error.Validation(
        code: "Filiation.SelfParent",
        description: "A Person cannot be their own parent."
    );

    public static readonly Error PersonNotInTree = Error.Validation(
        code: "Filiation.PersonNotInTree",
        description: "Both child and parent must be Persons in this tree."
    );

    public static readonly Error Duplicate = Error.Conflict(
        code: "Filiation.Duplicate",
        description: "This parent is already linked to this child."
    );
}
