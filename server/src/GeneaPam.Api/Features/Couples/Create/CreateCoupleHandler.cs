using ErrorOr;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Couples.Create;

public static class CreateCoupleHandler
{
    public static async Task<ErrorOr<CreateCoupleResponse>> Handle(
        CreateCoupleCommand command,
        CreateCoupleValidator validator,
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

        var couple = new Couple
        {
            TreeId = command.TreeId,
            PersonAId = command.PersonAId,
            PersonBId = command.PersonBId,
            Type = command.Type,
            CreatedBy = command.CreatedBy,
            CreatedAt = command.CreatedAt,
            UpdatedBy = command.UpdatedBy,
            UpdatedAt = command.UpdatedAt,
        };

        db.Couples.Add(couple);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateCoupleResponse(
            couple.Id,
            couple.TreeId,
            couple.PersonAId,
            couple.PersonBId,
            couple.Type
        );
    }
}
