using RazorLight;

namespace GeneaPam.Api.Infrastructure.Email;

public static class EmailExtensions
{
    public static IServiceCollection AddEmail(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));

        services.AddHttpClient<ResendClient>(
            "resend",
            (sp, client) =>
            {
                var options =
                    configuration.GetSection(ResendOptions.SectionName).Get<ResendOptions>()
                    ?? new ResendOptions();
                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
            }
        );

        services.AddSingleton<IRazorLightEngine>(sp =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            var templatesPath = Path.Combine(
                env.ContentRootPath,
                "Infrastructure",
                "Email",
                "Templates"
            );
            return new RazorLightEngineBuilder()
                .UseFileSystemProject(templatesPath)
                .UseMemoryCachingProvider()
                .Build();
        });

        services.AddSingleton<EmailRenderer>();

        return services;
    }
}
