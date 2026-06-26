using System.Security.Claims;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;

namespace GeneaPam.Api.Features.Persons.Get;

public sealed class GetPersonEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/trees/{treeId:guid}/persons/{id:guid}", HandleAsync)
            .RequireAuthorization()
            .WithTags("Persons")
            .Produces<GetPersonResponse>(StatusCodes.Status200OK)
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

        var result = await GetPersonQuery.Handle(db, treeId, id, userId, cancellationToken);

        return result.MatchToResponse(Results.Ok);
    }
}
