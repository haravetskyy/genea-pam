using GeneaPam.Api.Infrastructure.Audit;

namespace GeneaPam.Api.Features.Couples.AddFiliation;

public sealed record AddFiliationRequest(Guid ChildPersonId);

public sealed record AddFiliationResponse(Guid Id, Guid CoupleId, Guid ChildPersonId);

public sealed record AddFiliationCommand(
    Guid TreeId,
    Guid CoupleId,
    string OwnerId,
    Guid ChildPersonId
) : ICreateCommand, IUpdateCommand
{
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
