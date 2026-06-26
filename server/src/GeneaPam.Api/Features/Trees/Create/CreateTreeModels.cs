using GeneaPam.Api.Infrastructure.Audit;

namespace GeneaPam.Api.Features.Trees.Create;

public sealed record CreateTreeRequest(string Name, string? Description);

public sealed record CreateTreeResponse(Guid Id, string Name, string? Description);

public sealed record CreateTreeCommand(string OwnerId, string Name, string? Description)
    : ICreateCommand,
        IUpdateCommand
{
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
