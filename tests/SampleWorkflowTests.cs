using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Temporalio.Testing;
using Temporalio.Worker;
using NIWorker.Activities;
using NIWorker.Workflows;
using Xunit;

namespace NIWorker.Tests;

public class SampleWorkflowTests
{
    [Fact]
    public async Task SampleWorkflow_Should_Complete_Successfully()
    {
        var mockLogger = new Mock<ILogger<SampleActivities>>();
        var activities = new SampleActivities(mockLogger.Object);

        await using var env = await WorkflowEnvironment.StartLocalAsync();
        
        using var worker = new Temporalio.Worker.TemporalWorker(
            env.Client,
            new TemporalWorkerOptions("test-task-queue")
                .AddActivity(activities.GreetUser)
                .AddActivity(activities.ProcessData)
                .AddWorkflow<SampleWorkflow>());

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync(
                (SampleWorkflow wf) => wf.RunAsync(new WorkflowInput
                {
                    Name = "Test User",
                    Data = new List<string> { "item1", "item2" },
                    ProcessingType = "test"
                }),
                new(id: "test-workflow-1", taskQueue: "test-task-queue"));

            var result = await handle.GetResultAsync();
            
            result.Should().Contain("Hello, Test User!");
            result.Should().Contain("Processed 2 items");
            result.Should().Contain("status: Completed");
        });
    }

    [Fact]
    public async Task SampleWorkflow_Should_Handle_Empty_Data()
    {
        var mockLogger = new Mock<ILogger<SampleActivities>>();
        var activities = new SampleActivities(mockLogger.Object);

        await using var env = await WorkflowEnvironment.StartLocalAsync();
        
        using var worker = new Temporalio.Worker.TemporalWorker(
            env.Client,
            new TemporalWorkerOptions("test-task-queue")
                .AddActivity(activities.GreetUser)
                .AddActivity(activities.ProcessData)
                .AddWorkflow<SampleWorkflow>());

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync(
                (SampleWorkflow wf) => wf.RunAsync(new WorkflowInput
                {
                    Name = "Test User",
                    Data = new List<string>(),
                    ProcessingType = "test"
                }),
                new(id: "test-workflow-2", taskQueue: "test-task-queue"));

            var result = await handle.GetResultAsync();
            
            result.Should().Contain("Processed 0 items");
        });
    }
}