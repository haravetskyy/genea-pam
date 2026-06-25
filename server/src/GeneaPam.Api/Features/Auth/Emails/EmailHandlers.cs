using GeneaPam.Api.Infrastructure.Email;

namespace GeneaPam.Api.Features.Auth.Emails;

public sealed class WelcomeEmailHandler(EmailRenderer renderer, ResendClient resend)
{
    public async Task HandleAsync(WelcomeEmailJob job, CancellationToken cancellationToken)
    {
        var templateKey = $"welcome.{job.LanguagePreference}";
        var html = await renderer.RenderAsync(templateKey, new WelcomeEmailModel(job.UserName));
        var subject = job.LanguagePreference switch
        {
            "uk" => "Ласкаво просимо до Genea",
            "pl" => "Witaj w Genea",
            _ => "Welcome to Genea",
        };
        await resend.SendAsync(job.To, subject, html, cancellationToken);
    }
}

public sealed class PasswordResetEmailHandler(EmailRenderer renderer, ResendClient resend)
{
    public async Task HandleAsync(PasswordResetEmailJob job, CancellationToken cancellationToken)
    {
        var templateKey = $"password-reset.{job.LanguagePreference}";
        var model = new PasswordResetEmailModel(
            job.UserName,
            job.ResetLink,
            (int)job.Expiry.TotalHours
        );
        var html = await renderer.RenderAsync(templateKey, model);
        var subject = job.LanguagePreference switch
        {
            "uk" => "Скидання пароля",
            "pl" => "Resetowanie hasła",
            _ => "Reset Your Password",
        };
        await resend.SendAsync(job.To, subject, html, cancellationToken);
    }
}

public sealed class AccountDeletedEmailHandler(EmailRenderer renderer, ResendClient resend)
{
    public async Task HandleAsync(AccountDeletedEmailJob job, CancellationToken cancellationToken)
    {
        var templateKey = $"account-deleted.{job.LanguagePreference}";
        var html = await renderer.RenderAsync(
            templateKey,
            new AccountDeletedEmailModel(job.UserName)
        );
        var subject = job.LanguagePreference switch
        {
            "uk" => "Акаунт видалено",
            "pl" => "Konto usunięte",
            _ => "Your account has been deleted",
        };
        await resend.SendAsync(job.To, subject, html, cancellationToken);
    }
}
