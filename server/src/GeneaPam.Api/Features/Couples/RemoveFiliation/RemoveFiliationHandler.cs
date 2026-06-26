using ErrorOr;
using GeneaPam.Api.Features.Couples.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Couples.RemoveFiliation;

public static class RemoveFiliationHandler
{
    public static async Task<ErrorOr<Deleted>> Handle(
        RemoveFiliationCommand command,
        AppDbContext db,
        CancellationToken cancellationToken
    )
    {
        var treeOwned = await db.Trees.AnyAsync(
            t => t.Id == command.TreeId && t.OwnerId == command.OwnerId,
            cancellationToken
        );
        if (!treeOwned)
            return TreeErrors.NotFound;

        var filiation = await db.Filiations.FirstOrDefaultAsync(
            f => f.Id == command.Id && f.TreeId == command.TreeId,
            cancellationToken
        );
        if (filiation is null)
            return FiliationErrors.NotFound;

        db.Filiations.Remove(filiation);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
