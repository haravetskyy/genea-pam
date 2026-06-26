using ErrorOr;
using GeneaPam.Api.Features.Couples.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Couples.AddFiliation;

public static class AddFiliationHandler
{
    public static async Task<ErrorOr<AddFiliationResponse>> Handle(
        AddFiliationCommand command,
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

        var coupleExists = await db.Couples.AnyAsync(
            c => c.Id == command.CoupleId && c.TreeId == command.TreeId,
            cancellationToken
        );
        if (!coupleExists)
            return CoupleErrors.NotFound;

        var filiation = new Filiation
        {
            CoupleId = command.CoupleId,
            ChildPersonId = command.ChildPersonId,
            CreatedBy = command.CreatedBy,
            CreatedAt = command.CreatedAt,
            UpdatedBy = command.UpdatedBy,
            UpdatedAt = command.UpdatedAt,
        };

        db.Filiations.Add(filiation);
        await db.SaveChangesAsync(cancellationToken);

        return new AddFiliationResponse(filiation.Id, filiation.CoupleId, filiation.ChildPersonId);
    }
}
