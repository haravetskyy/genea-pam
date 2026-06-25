namespace GeneaPam.Api.Infrastructure.Audit;

public interface IUpdateCommand
{
    string UpdatedBy { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}
