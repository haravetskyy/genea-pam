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
}
