using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace GeneaPam.Api.Infrastructure.Audit;

public sealed class AuditBehavior(IHttpContextAccessor accessor)
{
    public void Before(object message)
    {
        var userId =
            accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var now = DateTimeOffset.UtcNow;

        if (message is ICreateCommand create)
        {
            create.CreatedBy = userId;
            create.CreatedAt = now;
        }

        if (message is IUpdateCommand update)
        {
            update.UpdatedBy = userId;
            update.UpdatedAt = now;
        }
    }
}
