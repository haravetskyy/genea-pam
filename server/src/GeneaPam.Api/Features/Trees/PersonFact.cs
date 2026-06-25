namespace GeneaPam.Api.Features.Trees;

public class PersonFact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TreeId { get; set; }
}
