using GeneaPam.Api.Infrastructure.Audit;

namespace GeneaPam.Api.Features.Couples.Create;

public sealed record CreateCoupleRequest(Guid PersonAId, Guid PersonBId);

public sealed record CreateCoupleResponse(
    Guid Id,
    Guid TreeId,
    Guid PersonAId,
    Guid PersonBId,
    string Type
);

public sealed record CreateCoupleCommand(
    Guid TreeId,
    string OwnerId,
    Guid PersonAId,
    Guid PersonBId
) : ICreateCommand, IUpdateCommand
{
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
