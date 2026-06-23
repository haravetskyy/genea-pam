using ErrorOr;

namespace GeneaPam.Api.Features.Auth;

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

    public static readonly Error PasswordTooShort = Error.Validation(
        code: "Auth.PasswordTooShort",
        description: "Password must be at least 8 characters."
    );

    public static readonly Error PasswordBreached = Error.Validation(
        code: "Auth.PasswordBreached",
        description: "This password has appeared in a data breach. Please choose a different password."
    );
}
