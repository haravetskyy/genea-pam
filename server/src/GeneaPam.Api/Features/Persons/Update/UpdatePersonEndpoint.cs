using System.Security.Claims;
using ErrorOr;
using GeneaPam.Api.Features.Persons.Internal;
using GeneaPam.Api.Infrastructure.Http;
using Wolverine;

namespace GeneaPam.Api.Features.Persons.Update;

public sealed class UpdatePersonEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut("/trees/{treeId:guid}/persons/{id:guid}", HandleAsync)
            .RequireAuthorization()
            .WithTags("Persons")
            .Produces<UpdatePersonResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
        Guid id,
        UpdatePersonRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var gender = GenderParsing.Parse(request.Gender);
        if (gender.IsError)
            return gender.Errors.ToProblemResult();

        var command = new UpdatePersonCommand(
            id,
            treeId,
            userId,
            request.FirstName,
            request.LastName,
            gender.Value,
            request.BirthDate,
            request.BirthDatePrecision,
            request.DeathDate,
            request.DeathDatePrecision
        );
        var result = await bus.InvokeAsync<ErrorOr<UpdatePersonResponse>>(
            command,
            cancellationToken
        );

        return result.MatchToResponse(Results.Ok);
    }
}
