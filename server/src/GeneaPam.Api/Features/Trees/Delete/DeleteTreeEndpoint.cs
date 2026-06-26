using System.Security.Claims;
using ErrorOr;
using GeneaPam.Api.Infrastructure.Http;
using Wolverine;

namespace GeneaPam.Api.Features.Trees.Delete;

public sealed class DeleteTreeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete("/trees/{id:guid}", HandleAsync)
            .RequireAuthorization()
            .WithTags("Trees")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var command = new DeleteTreeCommand(id, userId);
        var result = await bus.InvokeAsync<ErrorOr<Deleted>>(command, cancellationToken);

        return result.MatchToResponse(_ => Results.NoContent());
    }
}
