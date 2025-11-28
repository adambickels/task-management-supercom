using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Core.Interfaces;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;
using TaskManagement.Service;
using TaskManagement.Service.Services;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/service-log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting Task Management Service");

var builder = Host.CreateApplicationBuilder(args);

// Use Serilog as the logging provider
builder.Services.AddSerilog();

// Configure to run as Windows Service when deployed
builder.Services.AddWindowsService();

// Configure Database
builder.Services.AddDbContext<TaskManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<ITaskItemRepository, TaskItemRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();

// Register RabbitMQ service as singleton
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddRabbitMQ(serviceProvider =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var rabbitMQSection = configuration.GetSection("RabbitMQ");
        var factory = new RabbitMQ.Client.ConnectionFactory
        {
            HostName = rabbitMQSection.GetValue("HostName", "localhost"),
            UserName = rabbitMQSection.GetValue("UserName", "guest"),
            Password = rabbitMQSection.GetValue("Password", "guest"),
            VirtualHost = rabbitMQSection.GetValue("VirtualHost", "/"),
            Port = rabbitMQSection.GetValue("Port", 5672)
        };
        return factory.CreateConnectionAsync();
    }, name: "rabbitmq")
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "database");

// Register the worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
