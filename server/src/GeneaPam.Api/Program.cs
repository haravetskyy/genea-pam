using GeneaPam.Api.Configuration;
using GeneaPam.Api.Data;
using Microsoft.EntityFrameworkCore;
using Minio;
using Npgsql;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));
builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection(MinioOptions.SectionName));

var dbOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
    ?? throw new InvalidOperationException("Database configuration is missing");
var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
    ?? throw new InvalidOperationException("Redis configuration is missing");
var minioOptions = builder.Configuration.GetSection(MinioOptions.SectionName).Get<MinioOptions>()
    ?? throw new InvalidOperationException("Minio configuration is missing");

builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(dbOptions.ConnectionString).Build());
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dbOptions.ConnectionString));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions.ConnectionString));
builder.Services.AddMinio(c => c
    .WithEndpoint(minioOptions.Endpoint)
    .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
    .Build());

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
