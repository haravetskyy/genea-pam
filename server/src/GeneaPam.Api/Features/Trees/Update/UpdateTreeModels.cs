namespace GeneaPam.Api.Features.Trees.Update;

public sealed record UpdateTreeRequest(string Name, string? Description);

public sealed record UpdateTreeResponse(Guid Id, string Name, string? Description);
