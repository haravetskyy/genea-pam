using System.Security.Claims;
using GeneaPam.Api.Features.Trees.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> HandleAsync(
        Guid treeId,
        CreatePersonRequest request,
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

        var now = DateTimeOffset.UtcNow;
        var person = new Person
        {
            TreeId = treeId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Gender = request.Gender,
            BirthDate = request.BirthDate,
            BirthDatePrecision = request.BirthDatePrecision,
            DeathDate = request.DeathDate,
            DeathDatePrecision = request.DeathDatePrecision,
            CreatedBy = userId,
            CreatedAt = now,
            UpdatedBy = userId,
            UpdatedAt = now,
        };

        db.Persons.Add(person);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/trees/{treeId}/persons/{person.Id}",
            new CreatePersonResponse(
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
