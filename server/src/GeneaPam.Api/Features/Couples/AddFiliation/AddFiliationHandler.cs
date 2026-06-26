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

        if (command.ChildPersonId == command.ParentPersonId)
            return FiliationErrors.SelfParent;

        var bothInTree = await db
            .Persons.Where(p =>
                p.TreeId == command.TreeId
                && (p.Id == command.ChildPersonId || p.Id == command.ParentPersonId)
            )
            .CountAsync(cancellationToken);
        if (bothInTree != 2)
            return FiliationErrors.PersonNotInTree;

        var duplicate = await db.Filiations.AnyAsync(
            f =>
                f.ChildPersonId == command.ChildPersonId
                && f.ParentPersonId == command.ParentPersonId,
            cancellationToken
        );
        if (duplicate)
            return FiliationErrors.Duplicate;

        var filiation = new Filiation
        {
            TreeId = command.TreeId,
            ChildPersonId = command.ChildPersonId,
            ParentPersonId = command.ParentPersonId,
            ParentageType = command.ParentageType,
            CreatedBy = command.CreatedBy,
            CreatedAt = command.CreatedAt,
            UpdatedBy = command.UpdatedBy,
            UpdatedAt = command.UpdatedAt,
        };

        db.Filiations.Add(filiation);
        await db.SaveChangesAsync(cancellationToken);

        return new AddFiliationResponse(
            filiation.Id,
            filiation.TreeId,
            filiation.ChildPersonId,
            filiation.ParentPersonId,
            filiation.ParentageType
        );
    }
}
