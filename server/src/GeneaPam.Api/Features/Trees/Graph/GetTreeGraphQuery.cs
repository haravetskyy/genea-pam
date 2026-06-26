using ErrorOr;
using GeneaPam.Api.Features.Facts;
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

        var filiations = await db
            .Filiations.Where(f => f.TreeId == id)
            .ToListAsync(cancellationToken);

        // Birth/Death dates now live on the persons' Facts; resolve them per person.
        var dateFacts = await db
            .Facts.Where(f => f.TreeId == id && f.OwnerPersonId != null)
            .ToListAsync(cancellationToken);

        DateOnly? BirthOf(Guid personId) =>
            dateFacts
                .FirstOrDefault(f => f.OwnerPersonId == personId && f.Type == FactType.Birth)
                ?.DateValue;
        DateOnly? DeathOf(Guid personId) =>
            dateFacts
                .FirstOrDefault(f => f.OwnerPersonId == personId && f.Type == FactType.Death)
                ?.DateValue;

        var nodes = persons
            .Select(p => new GraphNode(
                p.Id,
                $"{p.FirstName} {p.LastName}",
                BirthOf(p.Id)?.Year,
                DeathOf(p.Id)?.Year,
                LivingStatus.From(BirthOf(p.Id), DeathOf(p.Id), p.ConfirmedDeceased)
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
            f.ParentPersonId,
            f.ChildPersonId
        ));

        var edges = coupleEdges.Concat(filiationEdges).ToList();

        return new GetTreeGraphResponse(nodes, edges);
    }
}
