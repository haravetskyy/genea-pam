using GeneaPam.Api.Infrastructure.Audit;

namespace GeneaPam.Api.Features.Couples.AddFiliation;

public sealed record AddFiliationRequest(
    Guid ChildPersonId,
    Guid ParentPersonId,
    string? ParentageType = null
);

public sealed record AddFiliationResponse(
    Guid Id,
    Guid TreeId,
    Guid ChildPersonId,
    Guid ParentPersonId,
    ParentageType ParentageType
);

public sealed record AddFiliationCommand(
    Guid TreeId,
    string OwnerId,
    Guid ChildPersonId,
    Guid ParentPersonId,
    ParentageType ParentageType
) : ICreateCommand, IUpdateCommand
{
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
