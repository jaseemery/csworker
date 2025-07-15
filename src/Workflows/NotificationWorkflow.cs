using Microsoft.Extensions.Logging;
using Temporalio.Workflows;
using NIWorker.Activities;

namespace NIWorker.Workflows;

[Workflow]
public class NotificationWorkflow
{
    [WorkflowRun]
    public async Task<NotificationResult> RunAsync(NotificationWorkflowInput input)
    {
        var logger = Workflow.Logger;
        logger.LogInformation("Starting notification workflow for user: {UserId}", input.UserId);

        var results = new List<string>();
        var emailSuccess = false;
        var smsSuccess = false;

        // Send email notification if email is provided
        if (!string.IsNullOrEmpty(input.Email))
        {
            try
            {
                emailSuccess = await Workflow.ExecuteActivityAsync(
                    (NotificationActivities act) => act.SendEmailNotification(new EmailNotificationRequest
                    {
                        Email = input.Email,
                        Subject = input.Subject,
                        Body = input.Message
                    }),
                    new ActivityOptions
                    {
                        StartToCloseTimeout = TimeSpan.FromMinutes(2),
                        RetryPolicy = new()
                        {
                            MaximumAttempts = 3,
                            BackoffCoefficient = 2,
                            InitialInterval = TimeSpan.FromSeconds(1)
                        }
                    });

                results.Add($"Email to {input.Email}: {(emailSuccess ? "Success" : "Failed")}");
            }
            catch (Exception ex)
            {
                logger.LogWarning("Email notification failed: {Error}", ex.Message);
                results.Add($"Email to {input.Email}: Failed - {ex.Message}");
            }
        }

        // Send SMS notification if phone number is provided
        if (!string.IsNullOrEmpty(input.PhoneNumber))
        {
            try
            {
                smsSuccess = await Workflow.ExecuteActivityAsync(
                    (NotificationActivities act) => act.SendSmsNotification(new SmsNotificationRequest
                    {
                        PhoneNumber = input.PhoneNumber,
                        Message = input.Message
                    }),
                    new ActivityOptions
                    {
                        StartToCloseTimeout = TimeSpan.FromMinutes(1),
                        RetryPolicy = new()
                        {
                            MaximumAttempts = 3,
                            BackoffCoefficient = 2,
                            InitialInterval = TimeSpan.FromSeconds(1)
                        }
                    });

                results.Add($"SMS to {input.PhoneNumber}: {(smsSuccess ? "Success" : "Failed")}");
            }
            catch (Exception ex)
            {
                logger.LogWarning("SMS notification failed: {Error}", ex.Message);
                results.Add($"SMS to {input.PhoneNumber}: Failed - {ex.Message}");
            }
        }

        // Log notification events
        if (!string.IsNullOrEmpty(input.Email))
        {
            await Workflow.ExecuteActivityAsync(
                (NotificationActivities act) => act.LogNotificationEvent("Email", input.Email, emailSuccess),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) });
        }

        if (!string.IsNullOrEmpty(input.PhoneNumber))
        {
            await Workflow.ExecuteActivityAsync(
                (NotificationActivities act) => act.LogNotificationEvent("SMS", input.PhoneNumber, smsSuccess),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) });
        }

        var overallSuccess = (string.IsNullOrEmpty(input.Email) || emailSuccess) && 
                           (string.IsNullOrEmpty(input.PhoneNumber) || smsSuccess);

        logger.LogInformation("Notification workflow completed for user {UserId}. Overall success: {Success}",
            input.UserId, overallSuccess);

        return new NotificationResult
        {
            UserId = input.UserId,
            Success = overallSuccess,
            Results = results,
            CompletedAt = DateTime.UtcNow
        };
    }
}

public record NotificationWorkflowInput
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public record NotificationResult
{
    public string UserId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public List<string> Results { get; init; } = new();
    public DateTime CompletedAt { get; init; }
}