using Wolverine;

namespace GeneaPam.Api.Infrastructure.Audit;

public static class AuditExtensions
{
    public static WolverineOptions AddAuditBehavior(this WolverineOptions opts)
    {
        opts.Policies.AddMiddleware<AuditBehavior>(chain =>
            typeof(ICreateCommand).IsAssignableFrom(chain.MessageType)
            || typeof(IUpdateCommand).IsAssignableFrom(chain.MessageType)
        );

        return opts;
    }
}
