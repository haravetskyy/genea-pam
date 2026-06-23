using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;

namespace GeneaPam.Api.Features.Auth;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    private static readonly HashSet<string> DisposableDomains = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "mailinator.com",
        "guerrillamail.com",
        "guerrillamail.net",
        "guerrillamail.org",
        "guerrillamail.biz",
        "guerrillamail.de",
        "guerrillamail.info",
        "guerrillamailblock.com",
        "throwam.com",
        "trashmail.com",
        "trashmail.me",
        "trashmail.net",
        "trashmail.org",
        "trashmail.io",
        "yopmail.com",
        "yopmail.fr",
        "spam4.me",
        "sharklasers.com",
        "guerrillamail.com",
        "grr.la",
        "guerrillamailblock.com",
        "fake-box.com",
        "tempmail.com",
        "temp-mail.org",
        "tempmail.net",
        "dispostable.com",
        "mailnull.com",
        "maildrop.cc",
        "mailnesia.com",
        "discard.email",
        "spamgourmet.com",
        "spamgourmet.net",
        "spamgourmet.org",
        "0-mail.com",
        "0815.ru",
        "10minutemail.com",
        "10minutemail.net",
        "20minutemail.com",
        "binkmail.com",
        "bobmail.info",
    };

    public RegisterValidator(IDnsResolver dnsResolver, IHttpClientFactory httpClientFactory)
    {
        RuleFor(r => r.Email)
            .MustAsync(
                async (email, ct) =>
                {
                    if (!IsValidEmailSyntax(email))
                        return false;
                    var domain = new MailAddress(email).Host;
                    if (DisposableDomains.Contains(domain))
                        return false;
                    return await dnsResolver.HasMxRecordAsync(domain, ct);
                }
            )
            .WithErrorCode(AuthErrors.EmailInvalid.Code)
            .WithMessage(AuthErrors.EmailInvalid.Description);

        RuleFor(r => r.Password)
            .MinimumLength(8)
            .WithErrorCode(AuthErrors.PasswordBreached.Code)
            .WithMessage("Password must be at least 8 characters.");

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
