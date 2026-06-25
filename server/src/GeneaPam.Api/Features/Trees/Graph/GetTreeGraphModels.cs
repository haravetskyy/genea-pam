namespace GeneaPam.Api.Features.Trees.Graph;

public sealed record GraphNode(
    Guid Id,
    string FullName,
    int? BirthYear,
    int? DeathYear,
    bool IsLiving
);

public sealed record GraphEdge(
    Guid Id,
    string Type,
    Guid? PersonAId,
    Guid? PersonBId,
    Guid? CoupleId,
    Guid? ChildId
);

public sealed record GetTreeGraphResponse(
    IReadOnlyList<GraphNode> Nodes,
    IReadOnlyList<GraphEdge> Edges
);
