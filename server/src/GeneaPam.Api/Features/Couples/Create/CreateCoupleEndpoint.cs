using System.Security.Claims;
using ErrorOr;
using GeneaPam.Api.Features.Couples.Internal;
using GeneaPam.Api.Infrastructure.Http;
using Wolverine;

namespace GeneaPam.Api.Features.Couples.Create;

public sealed class CreateCoupleEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/trees/{treeId:guid}/couples", HandleAsync)
            .RequireAuthorization()
            .WithTags("Couples")
            .Produces<CreateCoupleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
        CreateCoupleRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var type = CoupleTypeParsing.Parse(request.Type);
        if (type.IsError)
            return type.Errors.ToProblemResult();

        var command = new CreateCoupleCommand(
            treeId,
            userId,
            request.PersonAId,
            request.PersonBId,
            type.Value
        );
        var result = await bus.InvokeAsync<ErrorOr<CreateCoupleResponse>>(
            command,
            cancellationToken
        );

        return result.MatchToResponse(response =>
            Results.Created($"/trees/{treeId}/couples/{response.Id}", response)
        );
    }
}
