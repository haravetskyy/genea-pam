using System.Security.Claims;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;

namespace GeneaPam.Api.Features.Trees.Get;

public sealed class GetTreeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/trees/{id:guid}", HandleAsync)
            .RequireAuthorization()
            .WithTags("Trees")
            .Produces<GetTreeResponse>(StatusCodes.Status200OK)
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

        var result = await GetTreeQuery.Handle(db, id, userId, cancellationToken);

        return result.MatchToResponse(Results.Ok);
    }
}
