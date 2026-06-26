using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeneaPam.Api.Features.Facts;

/// <summary>
/// How precisely a Fact's date is known (ADR 0001). Smart enum (ADR 0006): a string-valued closed
/// set with <see cref="TryParse"/> returning <c>null</c> on an unknown value, so an invalid request
/// value is rejected by inline validation (422), not the serializer. Lives on the date Fact, so a
/// <c>null</c> precision can only co-occur with a present date — the "have-a-date-but-precision-
/// unknown vs no-date" conflation ADR 0007 warns about is avoided by construction (ADR 0007 case 2).
/// </summary>
[JsonConverter(typeof(DatePrecisionJsonConverter))]
public sealed class DatePrecision
{
    public static readonly DatePrecision FullDate = new("FullDate");
    public static readonly DatePrecision MonthYear = new("MonthYear");
    public static readonly DatePrecision YearOnly = new("YearOnly");
    public static readonly DatePrecision Approximate = new("Approximate");

    public static readonly IReadOnlyList<DatePrecision> All =
    [
        FullDate,
        MonthYear,
        YearOnly,
        Approximate,
    ];

    public string Value { get; }

    private DatePrecision(string value) => Value = value;

    public static DatePrecision? TryParse(string? value) =>
        value is null ? null : All.FirstOrDefault(p => p.Value == value);

    public override string ToString() => Value;
}

public sealed class DatePrecisionJsonConverter : JsonConverter<DatePrecision?>
{
    public override DatePrecision? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => reader.TokenType == JsonTokenType.Null ? null : DatePrecision.TryParse(reader.GetString());

    public override void Write(
        Utf8JsonWriter writer,
        DatePrecision? value,
        JsonSerializerOptions options
    )
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value);
    }
}
