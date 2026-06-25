using System.Security.Claims;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Trees.List;

public sealed class ListTreesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/trees", HandleAsync)
            .RequireAuthorization()
            .WithTags("Trees")
            .Produces<ListTreesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    internal static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var trees = await db
            .Trees.Where(t => t.OwnerId == userId)
            .Select(t => new TreeSummary(t.Id, t.Name, t.Description))
            .ToListAsync(cancellationToken);

        return Results.Ok(new ListTreesResponse(trees));
    }
}
