using System.Security.Claims;
using GeneaPam.Api.Features.Couples.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Couples.RemoveFiliation;

public sealed class RemoveFiliationEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(
                "/trees/{treeId:guid}/couples/{coupleId:guid}/filiations/{id:guid}",
                HandleAsync
            )
            .RequireAuthorization()
            .WithTags("Couples")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
        Guid coupleId,
        Guid id,
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var tree = await db.Trees.FirstOrDefaultAsync(
            t => t.Id == treeId && t.OwnerId == userId,
            cancellationToken
        );
        if (tree is null)
            return TreeErrors.NotFound.ToProblemResult();

        var couple = await db.Couples.FirstOrDefaultAsync(
            c => c.Id == coupleId && c.TreeId == treeId,
            cancellationToken
        );
        if (couple is null)
            return CoupleErrors.NotFound.ToProblemResult();

        var filiation = await db.Filiations.FirstOrDefaultAsync(
            f => f.Id == id && f.CoupleId == coupleId,
            cancellationToken
        );
        if (filiation is null)
            return FiliationErrors.NotFound.ToProblemResult();

        db.Filiations.Remove(filiation);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
