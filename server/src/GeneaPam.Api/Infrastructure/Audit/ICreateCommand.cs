namespace GeneaPam.Api.Infrastructure.Audit;

public interface ICreateCommand
{
    string CreatedBy { get; set; }
    DateTimeOffset CreatedAt { get; set; }
}
