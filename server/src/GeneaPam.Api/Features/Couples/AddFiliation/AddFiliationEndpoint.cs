using System.Security.Claims;
using ErrorOr;
using GeneaPam.Api.Infrastructure.Http;
using Wolverine;

namespace GeneaPam.Api.Features.Couples.AddFiliation;

public sealed class AddFiliationEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/trees/{treeId:guid}/couples/{coupleId:guid}/filiations", HandleAsync)
            .RequireAuthorization()
            .WithTags("Couples")
            .Produces<AddFiliationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
        Guid coupleId,
        AddFiliationRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var command = new AddFiliationCommand(treeId, coupleId, userId, request.ChildPersonId);
        var result = await bus.InvokeAsync<ErrorOr<AddFiliationResponse>>(
            command,
            cancellationToken
        );

        return result.MatchToResponse(response =>
            Results.Created(
                $"/trees/{treeId}/couples/{coupleId}/filiations/{response.Id}",
                response
            )
        );
    }
}
