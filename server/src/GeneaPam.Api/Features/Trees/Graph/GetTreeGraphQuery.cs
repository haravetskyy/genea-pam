using ErrorOr;
using GeneaPam.Api.Features.Persons;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Trees.Graph;

public static class GetTreeGraphQuery
{
    public static async Task<ErrorOr<GetTreeGraphResponse>> Handle(
        AppDbContext db,
        Guid id,
        string ownerId,
        CancellationToken cancellationToken
    )
    {
        var treeExists = await db.Trees.AnyAsync(
            t => t.Id == id && t.OwnerId == ownerId,
            cancellationToken
        );
        if (!treeExists)
            return TreeErrors.NotFound;

        var persons = await db.Persons.Where(p => p.TreeId == id).ToListAsync(cancellationToken);

        var couples = await db.Couples.Where(c => c.TreeId == id).ToListAsync(cancellationToken);

        var coupleIds = couples.Select(c => c.Id).ToList();
        var filiations = await db
            .Filiations.Where(f => coupleIds.Contains(f.CoupleId))
            .ToListAsync(cancellationToken);

        var nodes = persons
            .Select(p => new GraphNode(
                p.Id,
                $"{p.FirstName} {p.LastName}",
                p.BirthDate?.Year,
                p.DeathDate?.Year,
                LivingStatus.From(p.BirthDate, p.DeathDate, p.ConfirmedDeceased)
            ))
            .ToList();

        var coupleEdges = couples.Select(c => new GraphEdge(
            c.Id,
            "Couple",
            c.PersonAId,
            c.PersonBId,
            null,
            null
        ));

        var filiationEdges = filiations.Select(f => new GraphEdge(
            f.Id,
            "Filiation",
            null,
            null,
            f.CoupleId,
            f.ChildPersonId
        ));

        var edges = coupleEdges.Concat(filiationEdges).ToList();

        return new GetTreeGraphResponse(nodes, edges);
    }
}
