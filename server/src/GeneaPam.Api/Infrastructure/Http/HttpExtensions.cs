using System.Reflection;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace GeneaPam.Api.Infrastructure.Http;

public static class HttpExtensions
{
    public static IServiceCollection AddHttpInfrastructure(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer(
                (doc, _, _) =>
                {
                    doc.Info = new OpenApiInfo { Title = "GeneaPam API", Version = "v1" };
                    doc.Components ??= new OpenApiComponents();
                    doc.Components.SecuritySchemes ??=
                        new Dictionary<string, IOpenApiSecurityScheme>();
                    doc.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "Enter your JWT bearer token.",
                    };
                    return Task.CompletedTask;
                }
            );
        });

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
        app.MapOpenApi();
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
