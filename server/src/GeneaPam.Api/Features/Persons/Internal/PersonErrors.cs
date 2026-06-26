using ErrorOr;

namespace GeneaPam.Api.Features.Persons.Internal;

public static class PersonErrors
{
    public static readonly Error NotFound = Error.NotFound(
        code: "Person.NotFound",
        description: "Person not found."
    );

    public static readonly Error FirstNameRequired = Error.Validation(
        code: "Person.FirstNameRequired",
        description: "Person first name is required."
    );

    public static readonly Error GenderInvalid = Error.Validation(
        code: "Person.GenderInvalid",
        description: "Gender must be one of: Male, Female, Other."
    );

    public static readonly Error DuplicateBirthFact = Error.Validation(
        code: "Person.DuplicateBirthFact",
        description: "A Person can have at most one Birth fact."
    );

    public static readonly Error DuplicateDeathFact = Error.Validation(
        code: "Person.DuplicateDeathFact",
        description: "A Person can have at most one Death fact."
    );
}
