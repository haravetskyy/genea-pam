namespace GeneaPam.Api.Features.Trees.Create;

public sealed record CreateTreeRequest(string Name, string? Description);

public sealed record CreateTreeResponse(Guid Id, string Name, string? Description);
