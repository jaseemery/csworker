using Microsoft.Extensions.Logging;
using Temporalio.Activities;

namespace NIWorker.Activities;

public class NotificationActivities
{
    private readonly ILogger<NotificationActivities> _logger;

    public NotificationActivities(ILogger<NotificationActivities> logger)
    {
        _logger = logger;
    }

    [Activity]
    public async Task<bool> SendEmailNotification(EmailNotificationRequest request)
    {
        _logger.LogInformation("Sending email to {Email} with subject: {Subject}", 
            request.Email, request.Subject);

        // Simulate email sending
        await Task.Delay(500);

        _logger.LogInformation("Email sent successfully to {Email}", request.Email);
        return true;
    }

    [Activity]
    public async Task<bool> SendSmsNotification(SmsNotificationRequest request)
    {
        _logger.LogInformation("Sending SMS to {PhoneNumber}: {Message}", 
            request.PhoneNumber, request.Message);

        // Simulate SMS sending
        await Task.Delay(300);

        _logger.LogInformation("SMS sent successfully to {PhoneNumber}", request.PhoneNumber);
        return true;
    }

    [Activity]
    public async Task LogNotificationEvent(string notificationType, string recipient, bool success)
    {
        _logger.LogInformation("Logging notification event: {Type} to {Recipient}, Success: {Success}",
            notificationType, recipient, success);

        // Simulate database logging
        await Task.Delay(100);
    }
}

public record EmailNotificationRequest
{
    public string Email { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}

public record SmsNotificationRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}