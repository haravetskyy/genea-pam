namespace GeneaPam.Api.Features.Couples.Create;

public sealed record CreateCoupleRequest(Guid PersonAId, Guid PersonBId);

public sealed record CreateCoupleResponse(
    Guid Id,
    Guid TreeId,
    Guid PersonAId,
    Guid PersonBId,
    string Type
);
