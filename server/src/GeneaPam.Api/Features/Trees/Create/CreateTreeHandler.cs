using ErrorOr;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;

namespace GeneaPam.Api.Features.Trees.Create;

public static class CreateTreeHandler
{
    public static async Task<ErrorOr<CreateTreeResponse>> Handle(
        CreateTreeCommand command,
        CreateTreeValidator validator,
        AppDbContext db,
        CancellationToken cancellationToken
    )
    {
        var validated = await validator.ValidateToErrorOrAsync(command, cancellationToken);
        if (validated.IsError)
            return validated.Errors;

        var tree = new Tree
        {
            OwnerId = command.OwnerId,
            Name = command.Name,
            Description = command.Description,
            CreatedBy = command.CreatedBy,
            CreatedAt = command.CreatedAt,
            UpdatedBy = command.UpdatedBy,
            UpdatedAt = command.UpdatedAt,
        };

        db.Trees.Add(tree);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateTreeResponse(tree.Id, tree.Name, tree.Description);
    }
}
