using ErrorOr;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Trees.Update;

public static class UpdateTreeHandler
{
    public static async Task<ErrorOr<UpdateTreeResponse>> Handle(
        UpdateTreeCommand command,
        UpdateTreeValidator validator,
        AppDbContext db,
        CancellationToken cancellationToken
    )
    {
        var validated = await validator.ValidateToErrorOrAsync(command, cancellationToken);
        if (validated.IsError)
            return validated.Errors;

        var tree = await db.Trees.FirstOrDefaultAsync(
            t => t.Id == command.Id && t.OwnerId == command.OwnerId,
            cancellationToken
        );
        if (tree is null)
            return TreeErrors.NotFound;

        tree.Name = command.Name;
        tree.Description = command.Description;
        tree.UpdatedBy = command.UpdatedBy;
        tree.UpdatedAt = command.UpdatedAt;

        await db.SaveChangesAsync(cancellationToken);

        return new UpdateTreeResponse(tree.Id, tree.Name, tree.Description);
    }
}
