using ErrorOr;
using GeneaPam.Api.Features.Persons.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Persons.Delete;

public static class DeletePersonHandler
{
    public static async Task<ErrorOr<Deleted>> Handle(
        DeletePersonCommand command,
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

        var person = await db.Persons.FirstOrDefaultAsync(
            p => p.Id == command.Id && p.TreeId == command.TreeId,
            cancellationToken
        );
        if (person is null)
            return PersonErrors.NotFound;

        db.Persons.Remove(person);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
