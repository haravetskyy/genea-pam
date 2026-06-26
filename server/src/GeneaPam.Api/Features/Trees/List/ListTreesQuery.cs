using ErrorOr;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Trees.List;

public static class ListTreesQuery
{
    public static async Task<ErrorOr<ListTreesResponse>> Handle(
        AppDbContext db,
        string ownerId,
        CancellationToken cancellationToken
    )
    {
        var trees = await db
            .Trees.Where(t => t.OwnerId == ownerId)
            .Select(t => new TreeSummary(t.Id, t.Name, t.Description))
            .ToListAsync(cancellationToken);

        return new ListTreesResponse(trees);
    }
}
