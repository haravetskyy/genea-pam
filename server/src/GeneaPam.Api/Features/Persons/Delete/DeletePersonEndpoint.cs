using System.Security.Claims;
using GeneaPam.Api.Features.Persons.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Persons.Delete;

public sealed class DeletePersonEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete("/trees/{treeId:guid}/persons/{id:guid}", HandleAsync)
            .RequireAuthorization()
            .WithTags("Persons")
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

        var person = await db.Persons.FirstOrDefaultAsync(
            p => p.Id == id && p.TreeId == treeId,
            cancellationToken
        );
        if (person is null)
            return PersonErrors.NotFound.ToProblemResult();

        db.Persons.Remove(person);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
