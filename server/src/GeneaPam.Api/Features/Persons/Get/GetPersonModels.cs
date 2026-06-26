using GeneaPam.Api.Features.Facts;

namespace GeneaPam.Api.Features.Persons.Get;

public sealed record GetPersonResponse(
    Guid Id,
    Guid TreeId,
    string FirstName,
    string LastName,
    GenderType? Gender,
    bool ConfirmedDeceased,
    LivingStatus Status,
    IReadOnlyList<FactView> Facts
);
