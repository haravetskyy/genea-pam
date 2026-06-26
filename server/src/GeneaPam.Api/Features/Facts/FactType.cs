using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeneaPam.Api.Features.Facts;

/// <summary>Whether a <see cref="FactType"/> is a dated event or a stable attribute (ADR 0001).</summary>
public enum FactKind
{
    Event,
    Attribute,
}

/// <summary>
/// The kind of recorded Fact (ADR 0001). Smart enum (ADR 0006): a string-valued closed set with
/// <see cref="TryParse"/> returning <c>null</c> on an unknown value. Each member carries its
/// <see cref="FactKind"/> (Event = dated/placed, e.g. Birth; Attribute = stable text, e.g.
/// Occupation), which the domain validator uses to reject a date on an attribute fact. The
/// <see cref="Other"/> member supports free-text timeline events (§8.4) and requires a
/// <c>custom_label</c>.
/// </summary>
[JsonConverter(typeof(FactTypeJsonConverter))]
public sealed class FactType
{
    // Events (dated, place-bearing, timeline-feeding).
    public static readonly FactType Birth = new("Birth", FactKind.Event);
    public static readonly FactType Death = new("Death", FactKind.Event);
    public static readonly FactType Marriage = new("Marriage", FactKind.Event);
    public static readonly FactType Separation = new("Separation", FactKind.Event);
    public static readonly FactType Divorce = new("Divorce", FactKind.Event);

    // Attributes (stable, text_value-bearing).
    public static readonly FactType Occupation = new("Occupation", FactKind.Attribute);
    public static readonly FactType Nationality = new("Nationality", FactKind.Attribute);
    public static readonly FactType Religion = new("Religion", FactKind.Attribute);

    // Free-text escape hatch; requires a custom_label.
    public static readonly FactType Other = new("Other", FactKind.Attribute);

    public static readonly IReadOnlyList<FactType> All =
    [
        Birth,
        Death,
        Marriage,
        Separation,
        Divorce,
        Occupation,
        Nationality,
        Religion,
        Other,
    ];

    public string Value { get; }
    public FactKind Kind { get; }

    private FactType(string value, FactKind kind)
    {
        Value = value;
        Kind = kind;
    }

    public static FactType? TryParse(string? value) =>
        value is null ? null : All.FirstOrDefault(t => t.Value == value);

    public override string ToString() => Value;
}

public sealed class FactTypeJsonConverter : JsonConverter<FactType?>
{
    public override FactType? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => reader.TokenType == JsonTokenType.Null ? null : FactType.TryParse(reader.GetString());

    public override void Write(
        Utf8JsonWriter writer,
        FactType? value,
        JsonSerializerOptions options
    )
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value);
    }
}
