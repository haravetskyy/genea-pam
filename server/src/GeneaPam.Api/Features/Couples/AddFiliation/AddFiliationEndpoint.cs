using System.Security.Claims;
using ErrorOr;
using GeneaPam.Api.Features.Couples.Internal;
using GeneaPam.Api.Infrastructure.Http;
using Wolverine;

namespace GeneaPam.Api.Features.Couples.AddFiliation;

public sealed class AddFiliationEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/trees/{treeId:guid}/filiations", HandleAsync)
            .RequireAuthorization()
            .WithTags("Filiations")
            .Produces<AddFiliationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
        AddFiliationRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        ParentageType parentageType;
        if (request.ParentageType is null)
        {
            parentageType = ParentageType.Biological;
        }
        else
        {
            var parsed = ParentageType.TryParse(request.ParentageType);
            if (parsed is null)
                return FiliationErrors.ParentageTypeInvalid.ToProblemResult();
            parentageType = parsed;
        }

        var command = new AddFiliationCommand(
            treeId,
            userId,
            request.ChildPersonId,
            request.ParentPersonId,
            parentageType
        );
        var result = await bus.InvokeAsync<ErrorOr<AddFiliationResponse>>(
            command,
            cancellationToken
        );

        return result.MatchToResponse(response =>
            Results.Created($"/trees/{treeId}/filiations/{response.Id}", response)
        );
    }
}
