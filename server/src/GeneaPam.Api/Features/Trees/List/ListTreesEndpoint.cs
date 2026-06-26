using System.Security.Claims;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;

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

        var result = await ListTreesQuery.Handle(db, userId, cancellationToken);

        return result.MatchToResponse(Results.Ok);
    }
}
