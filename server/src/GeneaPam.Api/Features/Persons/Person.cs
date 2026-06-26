namespace GeneaPam.Api.Features.Persons;

public class Person
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TreeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public GenderType? Gender { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? BirthDatePrecision { get; set; }
    public DateOnly? DeathDate { get; set; }
    public string? DeathDatePrecision { get; set; }
    public bool ConfirmedDeceased { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
