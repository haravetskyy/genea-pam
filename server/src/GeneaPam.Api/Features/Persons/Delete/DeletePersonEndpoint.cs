using System.Security.Claims;
using ErrorOr;
using GeneaPam.Api.Infrastructure.Http;
using Wolverine;

namespace GeneaPam.Api.Features.Persons.Delete;

public sealed class DeletePersonEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete("/trees/{treeId:guid}/persons/{id:guid}", HandleAsync)
            .RequireAuthorization()
            .WithTags("Persons")
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

        var command = new DeletePersonCommand(id, treeId, userId);
        var result = await bus.InvokeAsync<ErrorOr<Deleted>>(command, cancellationToken);

        return result.MatchToResponse(_ => Results.NoContent());
    }
}
