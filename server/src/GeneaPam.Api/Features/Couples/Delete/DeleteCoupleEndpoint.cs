using System.Security.Claims;
using GeneaPam.Api.Features.Couples.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Couples.Delete;

public sealed class DeleteCoupleEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete("/trees/{treeId:guid}/couples/{id:guid}", HandleAsync)
            .RequireAuthorization()
            .WithTags("Couples")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
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
            c => c.Id == id && c.TreeId == treeId,
            cancellationToken
        );
        if (couple is null)
            return CoupleErrors.NotFound.ToProblemResult();

        db.Couples.Remove(couple);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
