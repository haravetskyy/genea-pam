using ErrorOr;
using GeneaPam.Api.Features.Couples.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Couples.Delete;

public static class DeleteCoupleHandler
{
    public static async Task<ErrorOr<Deleted>> Handle(
        DeleteCoupleCommand command,
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

        var couple = await db.Couples.FirstOrDefaultAsync(
            c => c.Id == command.Id && c.TreeId == command.TreeId,
            cancellationToken
        );
        if (couple is null)
            return CoupleErrors.NotFound;

        db.Couples.Remove(couple);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
