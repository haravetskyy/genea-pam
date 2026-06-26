namespace GeneaPam.Api.Features.Facts;

/// <summary>
/// A discrete, typed, sourceable record (ADR 0001). One physical table with a polymorphic owner —
/// exactly one of <see cref="OwnerPersonId"/> or <see cref="OwnerCoupleId"/> is set (DB CHECK).
/// Event facts carry a date/place; attribute facts carry <see cref="TextValue"/>. <c>is_primary</c>
/// is a column only in this pass — primary/alternate semantics arrive in a later slice.
/// </summary>
public class Fact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TreeId { get; set; }

    public FactType Type { get; set; } = FactType.Other;
    public string? CustomLabel { get; set; }

    // Polymorphic owner: exactly one is set (DB CHECK).
    public Guid? OwnerPersonId { get; set; }
    public Guid? OwnerCoupleId { get; set; }

    // Event value: date (+ precision) and place.
    public DateOnly? DateValue { get; set; }
    public DatePrecision? Precision { get; set; }
    public string? PlaceText { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }

    // Attribute value.
    public string? TextValue { get; set; }

    public bool IsPrimary { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
