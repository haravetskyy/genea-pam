using GeneaPam.Api.Infrastructure.Audit;

namespace GeneaPam.Api.Features.Persons.Update;

public sealed record UpdatePersonRequest(
    string FirstName,
    string LastName,
    string? Gender,
    DateOnly? BirthDate,
    string? BirthDatePrecision,
    DateOnly? DeathDate,
    string? DeathDatePrecision,
    bool ConfirmedDeceased = false
);

public sealed record UpdatePersonResponse(
    Guid Id,
    Guid TreeId,
    string FirstName,
    string LastName,
    GenderType? Gender,
    DateOnly? BirthDate,
    string? BirthDatePrecision,
    DateOnly? DeathDate,
    string? DeathDatePrecision,
    bool ConfirmedDeceased,
    LivingStatus Status
);

public sealed record UpdatePersonCommand(
    Guid Id,
    Guid TreeId,
    string OwnerId,
    string FirstName,
    string LastName,
    GenderType? Gender,
    DateOnly? BirthDate,
    string? BirthDatePrecision,
    DateOnly? DeathDate,
    string? DeathDatePrecision,
    bool ConfirmedDeceased
) : IUpdateCommand
{
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
