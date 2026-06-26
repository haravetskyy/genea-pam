using System.Security.Claims;
using ErrorOr;
using GeneaPam.Api.Infrastructure.Http;
using Wolverine;

namespace GeneaPam.Api.Features.Trees.Create;

public sealed class CreateTreeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/trees", HandleAsync)
            .RequireAuthorization()
            .WithTags("Trees")
            .Produces<CreateTreeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    internal static async Task<IResult> HandleAsync(
        CreateTreeRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var command = new CreateTreeCommand(userId, request.Name, request.Description);
        var result = await bus.InvokeAsync<ErrorOr<CreateTreeResponse>>(command, cancellationToken);

        return result.MatchToResponse(response =>
            Results.Created($"/trees/{response.Id}", response)
        );
    }
}
