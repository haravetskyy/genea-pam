using System.Security.Claims;
using GeneaPam.Api.Features.Persons.Internal;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Persons.Get;

public sealed class GetPersonEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/trees/{treeId:guid}/persons/{id:guid}", HandleAsync)
            .RequireAuthorization()
            .WithTags("Persons")
            .Produces<GetPersonResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
        Guid id,
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var tree = await db.Trees.FirstOrDefaultAsync(
            t => t.Id == treeId && t.OwnerId == userId,
            cancellationToken
        );
        if (tree is null)
            return TreeErrors.NotFound.ToProblemResult();

        var person = await db.Persons.FirstOrDefaultAsync(
            p => p.Id == id && p.TreeId == treeId,
            cancellationToken
        );
        if (person is null)
            return PersonErrors.NotFound.ToProblemResult();

        return Results.Ok(
            new GetPersonResponse(
                person.Id,
                person.TreeId,
                person.FirstName,
                person.LastName,
                person.Gender,
                person.BirthDate,
                person.BirthDatePrecision,
                person.DeathDate,
                person.DeathDatePrecision
            )
        );
    }
}
