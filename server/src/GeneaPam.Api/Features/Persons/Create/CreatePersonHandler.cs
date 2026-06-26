using ErrorOr;
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
            BirthDate = command.BirthDate,
            BirthDatePrecision = command.BirthDatePrecision,
            DeathDate = command.DeathDate,
            DeathDatePrecision = command.DeathDatePrecision,
            ConfirmedDeceased = command.ConfirmedDeceased,
            CreatedBy = command.CreatedBy,
            CreatedAt = command.CreatedAt,
            UpdatedBy = command.UpdatedBy,
            UpdatedAt = command.UpdatedAt,
        };

        db.Persons.Add(person);
        await db.SaveChangesAsync(cancellationToken);

        return new CreatePersonResponse(
            person.Id,
            person.TreeId,
            person.FirstName,
            person.LastName,
            person.Gender,
            person.BirthDate,
            person.BirthDatePrecision,
            person.DeathDate,
            person.DeathDatePrecision,
            person.ConfirmedDeceased,
            LivingStatus.From(person.BirthDate, person.DeathDate, person.ConfirmedDeceased)
        );
    }
}
