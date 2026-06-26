namespace GeneaPam.Api.Features.Couples.RemoveFiliation;

public sealed record RemoveFiliationCommand(Guid Id, Guid CoupleId, Guid TreeId, string OwnerId);
