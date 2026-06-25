using ErrorOr;
using FastEndpoints;

namespace GeneaPam.Api.Infrastructure.Http;

public sealed class ResponseSender : IGlobalPostProcessor
{
    public async Task PostProcessAsync(IPostProcessorContext context, CancellationToken ct)
    {
        if (context.Response is not IErrorOr errorOr)
            return;

        if (errorOr.IsError)
        {
            var problem = errorOr.Errors![0].ToProblemDetails();
            await context.HttpContext.Response.SendAsync(
                problem,
                problem.Status ?? StatusCodes.Status500InternalServerError,
                cancellation: ct
            );
            return;
        }

        var statusCode =
            context.HttpContext.Items.TryGetValue("ResponseStatusCode", out var code)
            && code is int sc
                ? sc
                : StatusCodes.Status200OK;

        var valueProperty = context.Response.GetType().GetProperty("Value");
        var value = valueProperty?.GetValue(context.Response);
        await context.HttpContext.Response.SendAsync(value, statusCode, cancellation: ct);
    }
}
