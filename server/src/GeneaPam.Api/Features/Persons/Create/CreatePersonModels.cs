using GeneaPam.Api.Features.Facts;
using GeneaPam.Api.Infrastructure.Audit;

namespace GeneaPam.Api.Features.Persons.Create;

public sealed record CreatePersonRequest(
    string FirstName,
    string LastName,
    string? Gender,
    bool ConfirmedDeceased = false,
    IReadOnlyList<FactInput>? Facts = null
);

public sealed record CreatePersonResponse(
    Guid Id,
    Guid TreeId,
    string FirstName,
    string LastName,
    GenderType? Gender,
    bool ConfirmedDeceased,
    LivingStatus Status,
    IReadOnlyList<FactView> Facts
);

public sealed record CreatePersonCommand(
    Guid TreeId,
    string OwnerId,
    string FirstName,
    string LastName,
    GenderType? Gender,
    bool ConfirmedDeceased,
    IReadOnlyList<Fact> Facts
) : ICreateCommand, IUpdateCommand
{
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
