using Microsoft.Extensions.Logging;
using Temporalio.Activities;

namespace CSWorker.Activities;

public class SampleActivities
{
    private readonly ILogger<SampleActivities> _logger;

    public SampleActivities(ILogger<SampleActivities> logger)
    {
        _logger = logger;
    }

    [Activity]
    public async Task<string> GreetUser(string name)
    {
        _logger.LogInformation("Greeting user: {Name}", name);
        
        await Task.Delay(1000);
        
        return $"Hello, {name}! Welcome to Temporal.";
    }

    [Activity]
    public async Task<ProcessDataResult> ProcessData(ProcessDataInput input)
    {
        _logger.LogInformation("Processing {Count} items with type: {Type}", 
            input.Data.Count, input.ProcessingType);

        var processedItems = new List<string>();
        var heartbeatData = new ProcessingProgress { TotalItems = input.Data.Count };

        for (int i = 0; i < input.Data.Count; i++)
        {
            if (ActivityExecutionContext.Current.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Activity cancelled at item {Index}", i);
                return new ProcessDataResult
                {
                    ProcessedItemCount = i,
                    Status = "Cancelled",
                    ProcessedData = processedItems
                };
            }

            await Task.Delay(500);
            
            var processedItem = $"Processed: {input.Data[i]} (Type: {input.ProcessingType})";
            processedItems.Add(processedItem);
            
            heartbeatData = heartbeatData with { ProcessedItems = i + 1, LastProcessedItem = input.Data[i] };
            ActivityExecutionContext.Current.Heartbeat(heartbeatData);
            
            _logger.LogDebug("Processed item {Index}: {Item}", i + 1, input.Data[i]);
        }

        _logger.LogInformation("Completed processing all {Count} items", input.Data.Count);

        return new ProcessDataResult
        {
            ProcessedItemCount = processedItems.Count,
            Status = "Completed",
            ProcessedData = processedItems
        };
    }
}

public record ProcessDataInput
{
    public List<string> Data { get; init; } = new();
    public string ProcessingType { get; init; } = "default";
}

public record ProcessDataResult
{
    public int ProcessedItemCount { get; init; }
    public string Status { get; init; } = string.Empty;
    public List<string> ProcessedData { get; init; } = new();
}

public record ProcessingProgress
{
    public int TotalItems { get; init; }
    public int ProcessedItems { get; init; }
    public string? LastProcessedItem { get; init; }
}