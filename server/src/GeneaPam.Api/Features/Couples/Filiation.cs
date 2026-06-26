namespace GeneaPam.Api.Features.Couples;

public class Filiation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TreeId { get; set; }
    public Guid ChildPersonId { get; set; }
    public Guid ParentPersonId { get; set; }
    public ParentageType ParentageType { get; set; } = ParentageType.Biological;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
