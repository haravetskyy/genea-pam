using GeneaPam.Api.Features.Auth;
using GeneaPam.Api.Infrastructure.Auth;
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
builder.Services.AddJwtBearer(builder.Configuration, builder.Environment);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddHttpInfrastructure();

var app = builder.Build();

app.UseHttpInfrastructure();
app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();

app.Run();

public partial class Program;
