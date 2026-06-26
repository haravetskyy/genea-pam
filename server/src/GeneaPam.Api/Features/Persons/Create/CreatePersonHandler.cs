using ErrorOr;
using GeneaPam.Api.Features.Persons.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Persons.Create;

public static class CreatePersonHandler
{
    public static async Task<ErrorOr<CreatePersonResponse>> Handle(
        CreatePersonCommand command,
        CreatePersonValidator validator,
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

        var person = new Person
        {
            TreeId = command.TreeId,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Gender = command.Gender,
            ConfirmedDeceased = command.ConfirmedDeceased,
            CreatedBy = command.CreatedBy,
            CreatedAt = command.CreatedAt,
            UpdatedBy = command.UpdatedBy,
            UpdatedAt = command.UpdatedAt,
        };

        db.Persons.Add(person);

        foreach (var fact in command.Facts)
        {
            fact.TreeId = command.TreeId;
            fact.OwnerPersonId = person.Id;
            fact.CreatedBy = command.CreatedBy;
            fact.CreatedAt = command.CreatedAt;
            fact.UpdatedBy = command.UpdatedBy;
            fact.UpdatedAt = command.UpdatedAt;
            db.Facts.Add(fact);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CreatePersonResponse(
            person.Id,
            person.TreeId,
            person.FirstName,
            person.LastName,
            person.Gender,
            person.ConfirmedDeceased,
            PersonFacts.StatusOf(command.Facts, person.ConfirmedDeceased),
            command.Facts.Select(PersonFacts.ToView).ToList()
        );
    }
}
