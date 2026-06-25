using System.Security.Claims;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Trees.Create;

public sealed class CreateTreeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/trees", HandleAsync)
            .RequireAuthorization()
            .WithTags("Trees")
            .Produces<CreateTreeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    internal static async Task<IResult> HandleAsync(
        CreateTreeRequest request,
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var now = DateTimeOffset.UtcNow;

        var tree = new Tree
        {
            OwnerId = userId,
            Name = request.Name,
            Description = request.Description,
            CreatedBy = userId,
            CreatedAt = now,
            UpdatedBy = userId,
            UpdatedAt = now,
        };

        db.Trees.Add(tree);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/trees/{tree.Id}",
            new CreateTreeResponse(tree.Id, tree.Name, tree.Description)
        );
    }
}
