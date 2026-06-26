using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Wolverine;

namespace GeneaPam.Api.Infrastructure.Audit;

public sealed class AuditBehavior(IHttpContextAccessor accessor)
{
    public void Before(Envelope envelope)
    {
        var userId =
            accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var now = DateTimeOffset.UtcNow;

        if (envelope.Message is ICreateCommand create)
        {
            create.CreatedBy = userId;
            create.CreatedAt = now;
        }

        if (envelope.Message is IUpdateCommand update)
        {
            update.UpdatedBy = userId;
            update.UpdatedAt = now;
        }
    }
}
