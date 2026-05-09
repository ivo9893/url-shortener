using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using URLShortener.Data;
using URLShortener.Infrastructure;
using URLShortener.Services;
using URLShortener.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var runMode = builder.Configuration.GetValue<string>("RUN_MODE");

builder.Services.AddSingleton<IdGeneratorService>();
builder.Services.AddSingleton<ShardRouter>();
builder.Services.AddSingleton<ShardedDbContextFactory>();


if (runMode == "migrator")
{
    var appMigration = builder.Build();

    using var scope = appMigration.Services.CreateScope();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    int physicalShards = configuration.GetValue<int>("Sharding:PhysicalShards");

    logger.LogInformation("Running migrations on {ShardCount} shards...", physicalShards);

    for (int i = 0; i < physicalShards; i++)
    {
        var connString = configuration[$"SHARD_{i}_CONNECTION"];
        if (string.IsNullOrEmpty(connString))
        {
            logger.LogWarning("Skipping shard {ShardIndex} - no connection string", i);
            continue;
        }

        logger.LogInformation("Migrating shard {ShardIndex}...", i);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connString);

        using var context = new ApplicationDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        logger.LogInformation("Shard {ShardIndex} migrated successfully", i);
    }

    logger.LogInformation("All migrations completed!");
    return;
}

if (runMode == "consumer")
{
    builder.Services.AddScoped<IClickAnalyticsService, ClickAnalyticsService>();
    builder.Services.AddHostedService<ClickAnalyticsConsumer>();

    var appConsumer = builder.Build();
    await appConsumer.RunAsync();
    return;
}

// API mode - add web services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IUrlShortenerService, UrlShortenerService>();
builder.Services.AddScoped<HealthCheckService>();
var redisConnection = builder.Configuration.GetValue<string>("REDIS_CONNECTION") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();

builder.Services.AddSingleton<IRabbitPublisher, RabbitPublisher>();
builder.Services.AddHostedService(sp => (RabbitPublisher)sp.GetRequiredService<IRabbitPublisher>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();

app.Run();