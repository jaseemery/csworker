using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Temporalio.Extensions.Hosting;
using ApiRequestor.Extensions;
using NIWorker.Activities;
using NIWorker.Workflows;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.AddConsole();

// Get Temporal configuration
var temporalEndpoint = builder.Configuration["Temporal:Endpoint"] ?? "localhost:7233";
var temporalNamespace = builder.Configuration["Temporal:Namespace"] ?? "default";
var taskQueue = builder.Configuration["Temporal:TaskQueue"] ?? "sample-task-queue";

builder.Services.AddLogging();

// Add ApiRequestor services
builder.Services.AddApiRequestor("api-config.json");

// Add Temporal worker with dependency injection
builder.Services
    .AddHostedTemporalWorker(
        temporalEndpoint,
        temporalNamespace,
        taskQueue)
    .AddScopedActivities<SampleActivities>()
    .AddScopedActivities<NotificationActivities>()
    .AddScopedActivities<ApiActivities>()
    .AddWorkflow<SampleWorkflow>()
    .AddWorkflow<NotificationWorkflow>()
    .AddWorkflow<UserManagementWorkflow>()
    .ConfigureOptions(options =>
    {
        // Configure worker options
        options.MaxConcurrentActivities = 100;
        options.MaxConcurrentWorkflowTasks = 50;
    });

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Temporal Worker...");
logger.LogInformation("Endpoint: {Endpoint}, Namespace: {Namespace}, TaskQueue: {TaskQueue}", 
    temporalEndpoint, temporalNamespace, taskQueue);

// Run the host (includes the Temporal worker)
await host.RunAsync();