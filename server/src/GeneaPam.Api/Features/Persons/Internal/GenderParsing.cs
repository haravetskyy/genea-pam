using ErrorOr;

namespace GeneaPam.Api.Features.Persons.Internal;

public static class GenderParsing
{
    /// <summary>
    /// Parses the raw gender string from a request into a <see cref="GenderType"/>.
    /// <c>null</c>/absent is valid and means "unknown". A present-but-unrecognized value is
    /// rejected with <see cref="PersonErrors.GenderInvalid"/> (RFC 9457 422).
    /// </summary>
    public static ErrorOr<GenderType?> Parse(string? raw)
    {
        if (raw is null)
            return (GenderType?)null;

        var parsed = GenderType.TryParse(raw);
        if (parsed is null)
            return PersonErrors.GenderInvalid;

        return parsed;
    }
}
