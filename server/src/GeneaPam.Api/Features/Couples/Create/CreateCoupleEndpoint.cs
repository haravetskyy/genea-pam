using System.Security.Claims;
using GeneaPam.Api.Features.Couples.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Couples.Create;

public sealed class CreateCoupleEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/trees/{treeId:guid}/couples", HandleAsync)
            .RequireAuthorization()
            .WithTags("Couples")
            .Produces<CreateCoupleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
        CreateCoupleRequest request,
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

        if (request.PersonAId == request.PersonBId)
            return CoupleErrors.SamePersonBothSides.ToProblemResult();

        var now = DateTimeOffset.UtcNow;
        var couple = new Couple
        {
            TreeId = treeId,
            PersonAId = request.PersonAId,
            PersonBId = request.PersonBId,
            Type = "Partner",
            CreatedBy = userId,
            CreatedAt = now,
            UpdatedBy = userId,
            UpdatedAt = now,
        };

        db.Couples.Add(couple);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/trees/{treeId}/couples/{couple.Id}",
            new CreateCoupleResponse(
                couple.Id,
                couple.TreeId,
                couple.PersonAId,
                couple.PersonBId,
                couple.Type
            )
        );
    }
}
