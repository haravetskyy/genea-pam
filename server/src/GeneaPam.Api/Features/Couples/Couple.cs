namespace GeneaPam.Api.Features.Couples;

public class Couple
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TreeId { get; set; }
    public Guid PersonAId { get; set; }
    public Guid PersonBId { get; set; }
    public string Type { get; set; } = "Partner";
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
