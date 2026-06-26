using ErrorOr;
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
        person.BirthDate = command.BirthDate;
        person.BirthDatePrecision = command.BirthDatePrecision;
        person.DeathDate = command.DeathDate;
        person.DeathDatePrecision = command.DeathDatePrecision;
        person.ConfirmedDeceased = command.ConfirmedDeceased;
        person.UpdatedBy = command.UpdatedBy;
        person.UpdatedAt = command.UpdatedAt;

        await db.SaveChangesAsync(cancellationToken);

        return new UpdatePersonResponse(
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
