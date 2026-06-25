using System.Security.Claims;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Trees.Graph;

public sealed class GetTreeGraphEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/trees/{id:guid}/graph", HandleAsync)
            .RequireAuthorization()
            .WithTags("Trees")
            .Produces<GetTreeGraphResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var tree = await db.Trees.FirstOrDefaultAsync(
            t => t.Id == id && t.OwnerId == userId,
            cancellationToken
        );
        if (tree is null)
            return TreeErrors.NotFound.ToProblemResult();

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
                p.DeathDate is null
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

        return Results.Ok(new GetTreeGraphResponse(nodes, edges));
    }
}
