using System.Security.Claims;
using ErrorOr;
using GeneaPam.Api.Infrastructure.Http;
using Wolverine;

namespace GeneaPam.Api.Features.Couples.Delete;

public sealed class DeleteCoupleEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete("/trees/{treeId:guid}/couples/{id:guid}", HandleAsync)
            .RequireAuthorization()
            .WithTags("Couples")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
        Guid id,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var command = new DeleteCoupleCommand(id, treeId, userId);
        var result = await bus.InvokeAsync<ErrorOr<Deleted>>(command, cancellationToken);

        return result.MatchToResponse(_ => Results.NoContent());
    }
}
