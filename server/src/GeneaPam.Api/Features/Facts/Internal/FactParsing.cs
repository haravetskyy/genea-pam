using ErrorOr;

namespace GeneaPam.Api.Features.Facts.Internal;

public static class FactParsing
{
    /// <summary>
    /// Parses and validates a single <see cref="FactInput"/> into a <see cref="Fact"/> shell — the
    /// owner, tree, and audit fields are left for the caller to stamp. Owner-agnostic so the Person
    /// and Couple flows share one rule set. Domain rules (ADR 0001): an attribute-kind fact may not
    /// carry a date; a fact of type <c>Other</c> requires a custom label.
    /// </summary>
    public static ErrorOr<Fact> Parse(FactInput input)
    {
        var type = FactType.TryParse(input.Type);
        if (type is null)
            return FactErrors.TypeInvalid;

        DatePrecision? precision = null;
        if (input.Precision is not null)
        {
            precision = DatePrecision.TryParse(input.Precision);
            if (precision is null)
                return FactErrors.PrecisionInvalid;
        }

        if (type.Kind == FactKind.Attribute && input.DateValue is not null)
            return FactErrors.DateOnAttribute;

        if (type == FactType.Other && string.IsNullOrWhiteSpace(input.CustomLabel))
            return FactErrors.CustomLabelRequired;

        return new Fact
        {
            Type = type,
            CustomLabel = input.CustomLabel,
            DateValue = input.DateValue,
            Precision = precision,
            PlaceText = input.PlaceText,
            Lat = input.Lat,
            Lng = input.Lng,
            TextValue = input.TextValue,
        };
    }
}
