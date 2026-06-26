using GeneaPam.Api.Infrastructure.Audit;

namespace GeneaPam.Api.Features.Persons.Create;

public sealed record CreatePersonRequest(
    string FirstName,
    string LastName,
    string? Gender,
    DateOnly? BirthDate,
    string? BirthDatePrecision,
    DateOnly? DeathDate,
    string? DeathDatePrecision
);

public sealed record CreatePersonResponse(
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

public sealed record CreatePersonCommand(
    Guid TreeId,
    string OwnerId,
    string FirstName,
    string LastName,
    string? Gender,
    DateOnly? BirthDate,
    string? BirthDatePrecision,
    DateOnly? DeathDate,
    string? DeathDatePrecision
) : ICreateCommand, IUpdateCommand
{
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
