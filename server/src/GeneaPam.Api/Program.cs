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
builder.Host.AddJobs(builder.Configuration);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
