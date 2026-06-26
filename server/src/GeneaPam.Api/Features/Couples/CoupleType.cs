using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeneaPam.Api.Features.Couples;

/// <summary>
/// The kind of union a Couple records (ADR 0004). Smart enum (ADR 0006): a string-valued closed
/// set with <see cref="TryParse"/> returning <c>null</c> on an unknown value, so an invalid request
/// value is rejected by inline validation (422), not the serializer. Non-nullable with a
/// <c>Partners</c> default — every union asserts a kind (ADR 0007: no "unknown" member, the field
/// is required); <c>Partners</c> is the neutral member.
/// </summary>
[JsonConverter(typeof(CoupleTypeJsonConverter))]
public sealed class CoupleType
{
    public static readonly CoupleType Married = new("Married");
    public static readonly CoupleType Partners = new("Partners");
    public static readonly CoupleType Separated = new("Separated");
    public static readonly CoupleType Divorced = new("Divorced");
    public static readonly CoupleType Other = new("Other");

    public static readonly IReadOnlyList<CoupleType> All =
    [
        Married,
        Partners,
        Separated,
        Divorced,
        Other,
    ];

    public string Value { get; }

    private CoupleType(string value) => Value = value;

    public static CoupleType? TryParse(string? value) =>
        value is null ? null : All.FirstOrDefault(c => c.Value == value);

    public override string ToString() => Value;
}

public sealed class CoupleTypeJsonConverter : JsonConverter<CoupleType>
{
    public override CoupleType Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => CoupleType.TryParse(reader.GetString()) ?? CoupleType.Partners;

    public override void Write(
        Utf8JsonWriter writer,
        CoupleType value,
        JsonSerializerOptions options
    ) => writer.WriteStringValue(value.Value);
}
