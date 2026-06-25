namespace GeneaPam.Api.Features.Persons.Get;

public sealed record GetPersonResponse(
    Guid Id,
    Guid TreeId,
    string FirstName,
    string LastName,
    string? Gender,
    DateOnly? BirthDate,
    string? BirthDatePrecision,
    DateOnly? DeathDate,
    string? DeathDatePrecision
);
