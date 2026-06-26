using ErrorOr;

namespace GeneaPam.Api.Features.Facts.Internal;

public static class FactErrors
{
    public static readonly Error TypeInvalid = Error.Validation(
        code: "Fact.TypeInvalid",
        description: "Type must be one of: Birth, Death, Marriage, Separation, Divorce, Occupation, Nationality, Religion, Other."
    );

    public static readonly Error PrecisionInvalid = Error.Validation(
        code: "Fact.PrecisionInvalid",
        description: "Precision must be one of: FullDate, MonthYear, YearOnly, Approximate."
    );

    public static readonly Error DateOnAttribute = Error.Validation(
        code: "Fact.DateOnAttribute",
        description: "An attribute fact cannot carry a date."
    );

    public static readonly Error CustomLabelRequired = Error.Validation(
        code: "Fact.CustomLabelRequired",
        description: "A fact of type Other requires a custom label."
    );
}
