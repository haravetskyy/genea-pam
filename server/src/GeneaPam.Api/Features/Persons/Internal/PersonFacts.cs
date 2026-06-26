using ErrorOr;
using GeneaPam.Api.Features.Facts;
using GeneaPam.Api.Features.Facts.Internal;

namespace GeneaPam.Api.Features.Persons.Internal;

/// <summary>
/// Person-flow rules for the Birth/Death facts written nested under a Person (#90 scope). In this
/// pass a Person may carry at most one Birth and one Death fact — multiple facts of one type, and
/// the primary/alternate designation that disambiguates them, are a later slice.
/// </summary>
public static class PersonFacts
{
    /// <summary>
    /// Parses each <see cref="FactInput"/> (delegating domain rules to <see cref="FactParsing"/>)
    /// and enforces the at-most-one-Birth / at-most-one-Death cardinality. Returns the parsed
    /// <see cref="Fact"/> shells (owner/tree/audit unset) on success.
    /// </summary>
    public static ErrorOr<List<Fact>> ParseForPerson(IReadOnlyList<FactInput> inputs)
    {
        var facts = new List<Fact>(inputs.Count);
        foreach (var input in inputs)
        {
            var parsed = FactParsing.Parse(input);
            if (parsed.IsError)
                return parsed.Errors;
            facts.Add(parsed.Value);
        }

        if (facts.Count(f => f.Type == FactType.Birth) > 1)
            return PersonErrors.DuplicateBirthFact;
        if (facts.Count(f => f.Type == FactType.Death) > 1)
            return PersonErrors.DuplicateDeathFact;

        return facts;
    }

    /// <summary>Living status derived from the Person's Birth/Death facts (ADR 0003).</summary>
    public static LivingStatus StatusOf(IEnumerable<Fact> facts, bool confirmedDeceased)
    {
        var list = facts as ICollection<Fact> ?? facts.ToList();
        var birth = list.FirstOrDefault(f => f.Type == FactType.Birth)?.DateValue;
        var death = list.FirstOrDefault(f => f.Type == FactType.Death)?.DateValue;
        return LivingStatus.From(birth, death, confirmedDeceased);
    }

    public static FactView ToView(Fact f) =>
        new(
            f.Id,
            f.Type,
            f.CustomLabel,
            f.DateValue,
            f.Precision,
            f.PlaceText,
            f.Lat,
            f.Lng,
            f.TextValue,
            f.IsPrimary
        );
}
