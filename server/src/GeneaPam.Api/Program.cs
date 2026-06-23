using GeneaPam.Api.Features.Auth;
using GeneaPam.Api.Infrastructure.Email;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Jobs;
using GeneaPam.Api.Infrastructure.Messaging;
using GeneaPam.Api.Infrastructure.Observability;
using GeneaPam.Api.Infrastructure.Persistence;
using GeneaPam.Api.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.AddObservability(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddMessaging(builder.Configuration);
builder.Services.AddStorage(builder.Configuration);
builder.Services.AddEmail(builder.Configuration);
builder.Host.AddJobs(builder.Configuration);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddHttpInfrastructure();

var app = builder.Build();

app.UseHttpInfrastructure();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapEndpoints();

if (!app.Environment.IsProduction())
    app.MapGet(
        "/test/throw",
        () =>
        {
            throw new InvalidOperationException("test exception");
        }
    );

app.Run();

public partial class Program;
