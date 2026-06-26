using System.Security.Claims;
using ErrorOr;
using GeneaPam.Api.Infrastructure.Http;
using Wolverine;

namespace GeneaPam.Api.Features.Couples.RemoveFiliation;

public sealed class RemoveFiliationEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(
                "/trees/{treeId:guid}/couples/{coupleId:guid}/filiations/{id:guid}",
                HandleAsync
            )
            .RequireAuthorization()
            .WithTags("Couples")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
        Guid coupleId,
        Guid id,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var command = new RemoveFiliationCommand(id, coupleId, treeId, userId);
        var result = await bus.InvokeAsync<ErrorOr<Deleted>>(command, cancellationToken);

        return result.MatchToResponse(_ => Results.NoContent());
    }
}
