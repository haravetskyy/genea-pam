using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Soenneker.Validators.Email.Disposable.Abstract;

namespace GeneaPam.Api.Features.Auth;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator(
        IDnsResolver dnsResolver,
        IHttpClientFactory httpClientFactory,
        IEmailDisposableValidator disposableValidator
    )
    {
        RuleFor(r => r.Email)
            .MustAsync(
                async (email, ct) =>
                {
                    if (!IsValidEmailSyntax(email))
                        return false;
                    var domain = new MailAddress(email).Host;
                    return await dnsResolver.HasMxRecordAsync(domain, ct);
                }
            )
            .WithErrorCode(AuthErrors.EmailInvalid.Code)
            .WithMessage(AuthErrors.EmailInvalid.Description);

        RuleFor(r => r.Email)
            .MustAsync(
                async (email, ct) =>
                {
                    if (!IsValidEmailSyntax(email))
                        return true; // syntax already caught by the rule above
                    return await disposableValidator.Validate(email, false, ct);
                }
            )
            .WithErrorCode(AuthErrors.EmailDisposable.Code)
            .WithMessage(AuthErrors.EmailDisposable.Description);

        RuleFor(r => r.Password)
            .MinimumLength(8)
            .WithErrorCode(AuthErrors.PasswordTooShort.Code)
            .WithMessage(AuthErrors.PasswordTooShort.Description);

        RuleFor(r => r.Password)
            .MustAsync(
                async (password, ct) =>
                {
                    var hash = BitConverter
                        .ToString(SHA1.HashData(Encoding.UTF8.GetBytes(password)))
                        .Replace("-", "")
                        .ToUpperInvariant();
                    var prefix = hash[..5];
                    var suffix = hash[5..];

                    var client = httpClientFactory.CreateClient("hibp");
                    var response = await client.GetAsync($"range/{prefix}", ct);
                    if (!response.IsSuccessStatusCode)
                        return true; // fail open on HIBP errors

                    var body = await response.Content.ReadAsStringAsync(ct);
                    return !body.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Any(line =>
                        {
                            var parts = line.Trim().Split(':');
                            return parts.Length == 2
                                && parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase)
                                && int.TryParse(parts[1], out var count)
                                && count > 0;
                        });
                }
            )
            .WithErrorCode(AuthErrors.PasswordBreached.Code)
            .WithMessage(AuthErrors.PasswordBreached.Description);
    }

    private static bool IsValidEmailSyntax(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
