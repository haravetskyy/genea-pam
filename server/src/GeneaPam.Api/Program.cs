using FastEndpoints;
using FastEndpoints.Swagger;
using GeneaPam.Api.Features.Auth;
using GeneaPam.Api.Infrastructure.Auth;
using GeneaPam.Api.Infrastructure.Cache;
using GeneaPam.Api.Infrastructure.Email;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Jobs;
using GeneaPam.Api.Infrastructure.Observability;
using GeneaPam.Api.Infrastructure.Persistence;
using GeneaPam.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.AddObservability(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddCache(builder.Configuration);
builder.Services.AddStorage(builder.Configuration);
builder.Services.AddEmail(builder.Configuration);
builder.Host.AddJobs(builder.Configuration);
builder.Services.AddJwtBearer(builder.Configuration, builder.Environment);
builder.Services.AddAuth(builder.Configuration);

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "GeneaPam API";
        s.Version = "v1";
        s.AddAuth(
            JwtBearerDefaults.AuthenticationScheme,
            new NSwag.OpenApiSecurityScheme
            {
                Type = NSwag.OpenApiSecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT bearer token.",
            }
        );
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseFastEndpoints();
app.UseOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("GeneaPam API");
    options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    options.AddPreferredSecuritySchemes("Bearer");
});
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.Run();

public partial class Program;
