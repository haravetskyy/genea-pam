using ErrorOr;

namespace GeneaPam.Api.Features.Couples.Internal;

public static class CoupleTypeParsing
{
    /// <summary>
    /// Parses the raw type string from a request into a <see cref="CoupleType"/>.
    /// <c>null</c>/absent defaults to <see cref="CoupleType.Partners"/> (the neutral member).
    /// A present-but-unrecognized value is rejected with <see cref="CoupleErrors.TypeInvalid"/>
    /// (RFC 9457 422).
    /// </summary>
    public static ErrorOr<CoupleType> Parse(string? raw)
    {
        if (raw is null)
            return CoupleType.Partners;

        var parsed = CoupleType.TryParse(raw);
        if (parsed is null)
            return CoupleErrors.TypeInvalid;

        return parsed;
    }
}
