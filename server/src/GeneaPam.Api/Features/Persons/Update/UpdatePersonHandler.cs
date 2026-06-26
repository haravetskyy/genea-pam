using ErrorOr;
using GeneaPam.Api.Features.Facts;
using GeneaPam.Api.Features.Persons.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Persons.Update;

public static class UpdatePersonHandler
{
    public static async Task<ErrorOr<UpdatePersonResponse>> Handle(
        UpdatePersonCommand command,
        UpdatePersonValidator validator,
        AppDbContext db,
        CancellationToken cancellationToken
    )
    {
        var validated = await validator.ValidateToErrorOrAsync(command, cancellationToken);
        if (validated.IsError)
            return validated.Errors;

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

        person.FirstName = command.FirstName;
        person.LastName = command.LastName;
        person.Gender = command.Gender;
        person.ConfirmedDeceased = command.ConfirmedDeceased;
        person.UpdatedBy = command.UpdatedBy;
        person.UpdatedAt = command.UpdatedAt;

        // Full-replace: the incoming facts are the person's complete fact set.
        var existing = await db
            .Facts.Where(f => f.OwnerPersonId == person.Id)
            .ToListAsync(cancellationToken);
        db.Facts.RemoveRange(existing);

        var newFacts = new List<Fact>(command.Facts.Count);
        foreach (var fact in command.Facts)
        {
            fact.TreeId = command.TreeId;
            fact.OwnerPersonId = person.Id;
            fact.CreatedBy = command.UpdatedBy;
            fact.CreatedAt = command.UpdatedAt;
            fact.UpdatedBy = command.UpdatedBy;
            fact.UpdatedAt = command.UpdatedAt;
            db.Facts.Add(fact);
            newFacts.Add(fact);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new UpdatePersonResponse(
            person.Id,
            person.TreeId,
            person.FirstName,
            person.LastName,
            person.Gender,
            person.ConfirmedDeceased,
            PersonFacts.StatusOf(newFacts, person.ConfirmedDeceased),
            newFacts.Select(PersonFacts.ToView).ToList()
        );
    }
}
