using System.Security.Claims;
using ErrorOr;
using GeneaPam.Api.Infrastructure.Http;
using Wolverine;

namespace GeneaPam.Api.Features.Trees.Update;

public sealed class UpdateTreeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut("/trees/{id:guid}", HandleAsync)
            .RequireAuthorization()
            .WithTags("Trees")
            .Produces<UpdateTreeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    internal static async Task<IResult> HandleAsync(
        Guid id,
        UpdateTreeRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var command = new UpdateTreeCommand(id, userId, request.Name, request.Description);
        var result = await bus.InvokeAsync<ErrorOr<UpdateTreeResponse>>(command, cancellationToken);

        return result.MatchToResponse(Results.Ok);
    }
}
