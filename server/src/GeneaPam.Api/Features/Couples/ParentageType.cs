using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeneaPam.Api.Features.Couples;

/// <summary>
/// Per-parent parentage on a Filiation (ADR 0004/0005). Smart enum (ADR 0006): a string-valued
/// closed set with <see cref="TryParse"/> returning <c>null</c> on an unknown value, so an
/// invalid request value is rejected by inline validation (422), not the serializer. Non-nullable
/// with a <c>Biological</c> default — every parent link asserts some parentage (ADR 0007: no
/// "unknown" member, the field is required).
/// </summary>
[JsonConverter(typeof(ParentageTypeJsonConverter))]
public sealed class ParentageType
{
    public static readonly ParentageType Biological = new("Biological");
    public static readonly ParentageType Adoptive = new("Adoptive");
    public static readonly ParentageType Step = new("Step");
    public static readonly ParentageType Foster = new("Foster");

    public static readonly IReadOnlyList<ParentageType> All = [Biological, Adoptive, Step, Foster];

    public string Value { get; }

    private ParentageType(string value) => Value = value;

    public static ParentageType? TryParse(string? value) =>
        value is null ? null : All.FirstOrDefault(p => p.Value == value);

    public override string ToString() => Value;
}

public sealed class ParentageTypeJsonConverter : JsonConverter<ParentageType>
{
    public override ParentageType Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => ParentageType.TryParse(reader.GetString()) ?? ParentageType.Biological;

    public override void Write(
        Utf8JsonWriter writer,
        ParentageType value,
        JsonSerializerOptions options
    ) => writer.WriteStringValue(value.Value);
}
