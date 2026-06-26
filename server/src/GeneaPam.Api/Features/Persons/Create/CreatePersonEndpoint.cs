using System.Security.Claims;
using ErrorOr;
using GeneaPam.Api.Features.Persons.Internal;
using GeneaPam.Api.Infrastructure.Http;
using Wolverine;

namespace GeneaPam.Api.Features.Persons.Create;

public sealed class CreatePersonEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/trees/{treeId:guid}/persons", HandleAsync)
            .RequireAuthorization()
            .WithTags("Persons")
            .Produces<CreatePersonResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
        CreatePersonRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var gender = GenderParsing.Parse(request.Gender);
        if (gender.IsError)
            return gender.Errors.ToProblemResult();

        var command = new CreatePersonCommand(
            treeId,
            userId,
            request.FirstName,
            request.LastName,
            gender.Value,
            request.BirthDate,
            request.BirthDatePrecision,
            request.DeathDate,
            request.DeathDatePrecision,
            request.ConfirmedDeceased
        );
        var result = await bus.InvokeAsync<ErrorOr<CreatePersonResponse>>(
            command,
            cancellationToken
        );

        return result.MatchToResponse(response =>
            Results.Created($"/trees/{treeId}/persons/{response.Id}", response)
        );
    }
}
