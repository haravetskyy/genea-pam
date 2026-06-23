using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace GeneaPam.Api.Infrastructure.Http;

public static class ErrorExtensions
{
    public static ProblemDetails ToProblemDetails(this Error error)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Bad Request"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
        };

        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = error.Description,
            Extensions = { ["errorCode"] = error.Code },
        };
    }

    public static IResult ToProblemResult(this Error error)
    {
        var problem = error.ToProblemDetails();
        return Results.Problem(
            detail: problem.Detail,
            statusCode: problem.Status,
            title: problem.Title,
            extensions: problem.Extensions.ToDictionary(k => k.Key, v => v.Value));
    }
}
