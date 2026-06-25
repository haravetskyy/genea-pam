using System.Security.Claims;
using GeneaPam.Api.Features.Couples.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Couples.AddFiliation;

public sealed class AddFiliationEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/trees/{treeId:guid}/couples/{coupleId:guid}/filiations", HandleAsync)
            .RequireAuthorization()
            .WithTags("Couples")
            .Produces<AddFiliationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
        Guid coupleId,
        AddFiliationRequest request,
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

        var now = DateTimeOffset.UtcNow;
        var filiation = new Filiation
        {
            CoupleId = coupleId,
            ChildPersonId = request.ChildPersonId,
            CreatedBy = userId,
            CreatedAt = now,
            UpdatedBy = userId,
            UpdatedAt = now,
        };

        db.Filiations.Add(filiation);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/trees/{treeId}/couples/{coupleId}/filiations/{filiation.Id}",
            new AddFiliationResponse(filiation.Id, filiation.CoupleId, filiation.ChildPersonId)
        );
    }
}
