using ErrorOr;

namespace GeneaPam.Api.Features.Auth.Internal;

public static class AuthErrors
{
    public static readonly Error EmailInvalid = Error.Validation(
        code: "Auth.EmailInvalid",
        description: "The email address is invalid or cannot receive mail."
    );

    public static readonly Error EmailAlreadyTaken = Error.Conflict(
        code: "Auth.EmailAlreadyTaken",
        description: "An account with this email address already exists."
    );

    public static readonly Error EmailDisposable = Error.Validation(
        code: "Auth.EmailDisposable",
        description: "Disposable email addresses are not allowed."
    );

    public static readonly Error PasswordTooShort = Error.Validation(
        code: "Auth.PasswordTooShort",
        description: "Password must be at least 8 characters."
    );

    public static readonly Error PasswordBreached = Error.Validation(
        code: "Auth.PasswordBreached",
        description: "This password has appeared in a data breach. Please choose a different password."
    );

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        code: "Auth.InvalidCredentials",
        description: "Invalid email or password."
    );

    public static readonly Error TokenInvalid = Error.Unauthorized(
        code: "Auth.TokenInvalid",
        description: "The refresh token is invalid or has expired."
    );
}
