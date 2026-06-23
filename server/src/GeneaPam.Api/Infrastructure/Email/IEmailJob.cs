namespace GeneaPam.Api.Infrastructure.Email;

public interface IEmailJob
{
    string To { get; }
    string UserName { get; }
    string LanguagePreference { get; }
}
