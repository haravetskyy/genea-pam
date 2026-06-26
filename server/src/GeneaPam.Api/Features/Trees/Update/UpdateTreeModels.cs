using GeneaPam.Api.Infrastructure.Audit;

namespace GeneaPam.Api.Features.Trees.Update;

public sealed record UpdateTreeRequest(string Name, string? Description);

public sealed record UpdateTreeResponse(Guid Id, string Name, string? Description);

public sealed record UpdateTreeCommand(Guid Id, string OwnerId, string Name, string? Description)
    : IUpdateCommand
{
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
