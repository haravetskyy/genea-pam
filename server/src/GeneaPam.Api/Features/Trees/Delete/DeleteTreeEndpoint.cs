using System.Security.Claims;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Trees.Delete;

public sealed class DeleteTreeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete("/trees/{id:guid}", HandleAsync)
            .RequireAuthorization()
            .WithTags("Trees")
            .Produces(StatusCodes.Status204NoContent)
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

        db.Trees.Remove(tree);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
