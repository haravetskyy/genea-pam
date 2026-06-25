namespace GeneaPam.Api.Features.Trees.List;

public sealed record TreeSummary(Guid Id, string Name, string? Description);

public sealed record ListTreesResponse(IReadOnlyList<TreeSummary> Trees);
