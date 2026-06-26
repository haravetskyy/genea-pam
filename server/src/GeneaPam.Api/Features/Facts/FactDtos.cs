namespace GeneaPam.Api.Features.Facts;

/// <summary>
/// A Fact as written through an owner flow (e.g. nested under a Person create). The owner is
/// implicit from the enclosing request; <c>is_primary</c> and citations are not part of this pass.
/// <c>Type</c>/<c>Precision</c> are raw strings here and parsed at the endpoint (RFC 9457 422 on an
/// unknown value), per the smart-enum convention (ADR 0006).
/// </summary>
public sealed record FactInput(
    string Type,
    DateOnly? DateValue = null,
    string? Precision = null,
    string? PlaceText = null,
    double? Lat = null,
    double? Lng = null,
    string? CustomLabel = null,
    string? TextValue = null
);

/// <summary>A Fact as returned in a read (server-set <see cref="Id"/>; typed enums).</summary>
public sealed record FactView(
    Guid Id,
    FactType Type,
    string? CustomLabel,
    DateOnly? DateValue,
    DatePrecision? Precision,
    string? PlaceText,
    double? Lat,
    double? Lng,
    string? TextValue,
    bool IsPrimary
);
