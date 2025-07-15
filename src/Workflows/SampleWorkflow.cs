using Microsoft.Extensions.Logging;
using Temporalio.Workflows;
using NIWorker.Activities;

namespace NIWorker.Workflows;

[Workflow]
public class SampleWorkflow
{
    [WorkflowRun]
    public async Task<string> RunAsync(WorkflowInput input)
    {
        var logger = Workflow.Logger;
        logger.LogInformation("Workflow started with input: {Name}", input.Name);

        var greetingResult = await Workflow.ExecuteActivityAsync(
            (SampleActivities act) => act.GreetUser(input.Name),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromSeconds(30),
                RetryPolicy = new()
                {
                    MaximumAttempts = 3,
                    BackoffCoefficient = 2,
                    InitialInterval = TimeSpan.FromSeconds(1),
                    MaximumInterval = TimeSpan.FromSeconds(10)
                }
            });

        logger.LogInformation("Greeting completed: {Result}", greetingResult);

        await Workflow.DelayAsync(TimeSpan.FromSeconds(2));

        var processedData = await Workflow.ExecuteActivityAsync(
            (SampleActivities act) => act.ProcessData(new ProcessDataInput 
            { 
                Data = input.Data ?? new List<string>(),
                ProcessingType = input.ProcessingType ?? "default"
            }),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromMinutes(5),
                HeartbeatTimeout = TimeSpan.FromSeconds(30),
                RetryPolicy = new()
                {
                    MaximumAttempts = 5,
                    BackoffCoefficient = 2,
                    InitialInterval = TimeSpan.FromSeconds(1),
                    MaximumInterval = TimeSpan.FromSeconds(30)
                }
            });

        logger.LogInformation("Data processing completed. Processed {Count} items", processedData.ProcessedItemCount);

        return $"Workflow completed! {greetingResult} Processed {processedData.ProcessedItemCount} items with status: {processedData.Status}";
    }
}

public record WorkflowInput
{
    public string Name { get; init; } = string.Empty;
    public List<string>? Data { get; init; }
    public string? ProcessingType { get; init; }
}