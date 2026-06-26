using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeneaPam.Api.Features.Persons;

/// <summary>
/// Derived three-state living status (ADR 0003). Built as a smart enum (ADR 0006) for clean
/// string serialization, but — unlike request-bound enums — it is never parsed from untrusted
/// input, so it has no <c>TryParse</c>/<c>CHECK</c> machinery. <c>Unknown</c> is a real derived
/// state (Rule X), an explicit member rather than an absence sentinel (ADR 0007 cases 1 + 3).
/// </summary>
[JsonConverter(typeof(LivingStatusJsonConverter))]
public sealed class LivingStatus
{
    public static readonly LivingStatus Living = new("Living");
    public static readonly LivingStatus Deceased = new("Deceased");
    public static readonly LivingStatus Unknown = new("Unknown");

    public string Value { get; }

    private LivingStatus(string value) => Value = value;

    /// <summary>
    /// The single definition of Rule X (ADR 0003) — pure presence-of-data, no age heuristic.
    /// Deceased: death date present OR confirmed deceased. Living: not deceased AND birth date
    /// present. Unknown: not deceased AND no birth date.
    /// </summary>
    public static LivingStatus From(
        DateOnly? birthDate,
        DateOnly? deathDate,
        bool confirmedDeceased
    )
    {
        if (deathDate is not null || confirmedDeceased)
            return Deceased;

        return birthDate is not null ? Living : Unknown;
    }

    public override string ToString() => Value;
}

public sealed class LivingStatusJsonConverter : JsonConverter<LivingStatus>
{
    public override LivingStatus Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) =>
        reader.GetString() switch
        {
            "Living" => LivingStatus.Living,
            "Deceased" => LivingStatus.Deceased,
            _ => LivingStatus.Unknown,
        };

    public override void Write(
        Utf8JsonWriter writer,
        LivingStatus value,
        JsonSerializerOptions options
    ) => writer.WriteStringValue(value.Value);
}
