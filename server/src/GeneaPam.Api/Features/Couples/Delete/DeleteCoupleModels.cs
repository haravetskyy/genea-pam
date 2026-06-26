namespace GeneaPam.Api.Features.Couples.Delete;

public sealed record DeleteCoupleCommand(Guid Id, Guid TreeId, string OwnerId);
