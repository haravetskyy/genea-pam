using ErrorOr;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Trees.Delete;

public static class DeleteTreeHandler
{
    public static async Task<ErrorOr<Deleted>> Handle(
        DeleteTreeCommand command,
        AppDbContext db,
        CancellationToken cancellationToken
    )
    {
        var tree = await db.Trees.FirstOrDefaultAsync(
            t => t.Id == command.Id && t.OwnerId == command.OwnerId,
            cancellationToken
        );
        if (tree is null)
            return TreeErrors.NotFound;

        db.Trees.Remove(tree);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
