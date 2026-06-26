using GeneaPam.Api.Features.Persons;

namespace GeneaPam.Api.Features.Trees.Graph;

public sealed record GraphNode(
    Guid Id,
    string FullName,
    int? BirthYear,
    int? DeathYear,
    LivingStatus Status
);

public sealed record GraphEdge(
    Guid Id,
    string Type,
    Guid? PersonAId,
    Guid? PersonBId,
    Guid? ParentPersonId,
    Guid? ChildPersonId
);

public sealed record GetTreeGraphResponse(
    IReadOnlyList<GraphNode> Nodes,
    IReadOnlyList<GraphEdge> Edges
);
