using System.Reflection;
using Scalar.AspNetCore;

namespace GeneaPam.Api.Infrastructure.Http;

public static class HttpExtensions
{
    public static IServiceCollection AddHttpInfrastructure(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        var endpointTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                t is { IsAbstract: false, IsInterface: false }
                && t.IsAssignableTo(typeof(IEndpoint))
            );

        foreach (var type in endpointTypes)
            services.AddSingleton(typeof(IEndpoint), type);

        return services;
    }

    public static WebApplication UseHttpInfrastructure(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("GeneaPam API");
            options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            options.WithPreferredScheme("Bearer");
        });

        return app;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var endpoints = app.Services.GetServices<IEndpoint>();
        foreach (var endpoint in endpoints)
            endpoint.MapEndpoints(app);

        return app;
    }
}
