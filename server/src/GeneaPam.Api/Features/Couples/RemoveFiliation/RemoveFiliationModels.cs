namespace GeneaPam.Api.Features.Couples.RemoveFiliation;

public sealed record RemoveFiliationCommand(Guid Id, Guid TreeId, string OwnerId);
