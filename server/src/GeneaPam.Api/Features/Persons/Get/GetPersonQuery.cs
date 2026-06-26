using ErrorOr;
using GeneaPam.Api.Features.Persons.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Persons.Get;

public static class GetPersonQuery
{
    public static async Task<ErrorOr<GetPersonResponse>> Handle(
        AppDbContext db,
        Guid treeId,
        Guid id,
        string ownerId,
        CancellationToken cancellationToken
    )
    {
        var treeOwned = await db.Trees.AnyAsync(
            t => t.Id == treeId && t.OwnerId == ownerId,
            cancellationToken
        );
        if (!treeOwned)
            return TreeErrors.NotFound;

        var person = await db
            .Persons.Where(p => p.Id == id && p.TreeId == treeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (person is null)
            return PersonErrors.NotFound;

        var facts = await db
            .Facts.Where(f => f.OwnerPersonId == person.Id)
            .ToListAsync(cancellationToken);

        return new GetPersonResponse(
            person.Id,
            person.TreeId,
            person.FirstName,
            person.LastName,
            person.Gender,
            person.ConfirmedDeceased,
            PersonFacts.StatusOf(facts, person.ConfirmedDeceased),
            facts.Select(PersonFacts.ToView).ToList()
        );
    }
}
