using GeneaPam.Api.Features.Facts;
using GeneaPam.Api.Infrastructure.Audit;

namespace GeneaPam.Api.Features.Persons.Update;

public sealed record UpdatePersonRequest(
    string FirstName,
    string LastName,
    string? Gender,
    bool ConfirmedDeceased = false,
    IReadOnlyList<FactInput>? Facts = null
);

public sealed record UpdatePersonResponse(
    Guid Id,
    Guid TreeId,
    string FirstName,
    string LastName,
    GenderType? Gender,
    bool ConfirmedDeceased,
    LivingStatus Status,
    IReadOnlyList<FactView> Facts
);

public sealed record UpdatePersonCommand(
    Guid Id,
    Guid TreeId,
    string OwnerId,
    string FirstName,
    string LastName,
    GenderType? Gender,
    bool ConfirmedDeceased,
    IReadOnlyList<Fact> Facts
) : IUpdateCommand
{
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
