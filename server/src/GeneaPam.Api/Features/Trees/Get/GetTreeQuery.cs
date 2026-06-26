using ErrorOr;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Trees.Get;

public static class GetTreeQuery
{
    public static async Task<ErrorOr<GetTreeResponse>> Handle(
        AppDbContext db,
        Guid id,
        string ownerId,
        CancellationToken cancellationToken
    )
    {
        var tree = await db
            .Trees.Where(t => t.Id == id && t.OwnerId == ownerId)
            .Select(t => new GetTreeResponse(t.Id, t.Name, t.Description))
            .FirstOrDefaultAsync(cancellationToken);

        if (tree is null)
            return TreeErrors.NotFound;

        return tree;
    }
}
