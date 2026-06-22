using Minio;
using Npgsql;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var postgresConn = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
    ?? throw new InvalidOperationException("POSTGRES_CONNECTION_STRING is not set");
var redisConn = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
    ?? throw new InvalidOperationException("REDIS_CONNECTION_STRING is not set");
var minioEndpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT")
    ?? throw new InvalidOperationException("MINIO_ENDPOINT is not set");
var minioAccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY")
    ?? throw new InvalidOperationException("MINIO_ACCESS_KEY is not set");
var minioSecretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY")
    ?? throw new InvalidOperationException("MINIO_SECRET_KEY is not set");

builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(postgresConn).Build());
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
builder.Services.AddMinio(c => c
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioAccessKey, minioSecretKey)
    .Build());

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
