using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeneaPam.Api.Features.Persons;

/// <summary>
/// Closed set of recorded genders (ADR 0004). Implemented as a smart enum: a string-valued
/// closed set with a private constructor and a <see cref="TryParse"/> that returns <c>null</c>
/// on an unrecognized value, so untrusted input is rejected by inline validation rather than
/// the JSON serializer. <c>null</c> (the absence of a value) means "unknown / not recorded" —
/// there is no <c>Unknown</c> member.
/// </summary>
[JsonConverter(typeof(GenderTypeJsonConverter))]
public sealed class GenderType
{
    public static readonly GenderType Male = new("Male");
    public static readonly GenderType Female = new("Female");
    public static readonly GenderType Other = new("Other");

    public static readonly IReadOnlyList<GenderType> All = [Male, Female, Other];

    public string Value { get; }

    private GenderType(string value) => Value = value;

    /// <summary>Returns the member with this exact value, or <c>null</c> if none matches.</summary>
    public static GenderType? TryParse(string? value) =>
        value is null ? null : All.FirstOrDefault(g => g.Value == value);

    public override string ToString() => Value;
}

public sealed class GenderTypeJsonConverter : JsonConverter<GenderType?>
{
    public override GenderType? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => reader.TokenType == JsonTokenType.Null ? null : GenderType.TryParse(reader.GetString());

    public override void Write(
        Utf8JsonWriter writer,
        GenderType? value,
        JsonSerializerOptions options
    )
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value);
    }
}
