using ErrorOr;

namespace GeneaPam.Api.Features.Trees.Internal;

public static class TreeErrors
{
    public static readonly Error NotFound = Error.NotFound(
        code: "Tree.NotFound",
        description: "Tree not found."
    );
}
