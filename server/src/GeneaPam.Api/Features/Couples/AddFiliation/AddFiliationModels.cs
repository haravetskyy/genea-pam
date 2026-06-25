namespace GeneaPam.Api.Features.Couples.AddFiliation;

public sealed record AddFiliationRequest(Guid ChildPersonId);

public sealed record AddFiliationResponse(Guid Id, Guid CoupleId, Guid ChildPersonId);
