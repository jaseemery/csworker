using Microsoft.Extensions.Logging;
using Temporalio.Workflows;
using NIWorker.Activities;

namespace NIWorker.Workflows;

[Workflow]
public class UserManagementWorkflow
{
    [WorkflowRun]
    public async Task<UserManagementResult> RunAsync(UserManagementInput input)
    {
        var logger = Workflow.Logger;
        logger.LogInformation("Starting user management workflow for user: {UserId}", input.UserId);

        var results = new List<string>();
        var errors = new List<string>();

        try
        {
            // Step 1: Get user information
            logger.LogInformation("Fetching user information...");
            var userResult = await Workflow.ExecuteActivityAsync(
                (ApiActivities act) => act.GetUserByIdAsync(input.UserId),
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

            if (!userResult.Success)
            {
                errors.Add($"Failed to fetch user: {userResult.Message}");
                return new UserManagementResult
                {
                    Success = false,
                    UserId = input.UserId,
                    Results = results,
                    Errors = errors,
                    CompletedAt = DateTime.UtcNow
                };
            }

            results.Add($"Successfully retrieved user: {userResult.User?.Name} ({userResult.User?.Email})");

            // Step 2: Get user's existing posts
            logger.LogInformation("Fetching user's existing posts...");
            var postsResult = await Workflow.ExecuteActivityAsync(
                (ApiActivities act) => act.GetUserPostsAsync(input.UserId),
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

            if (postsResult.Success)
            {
                results.Add($"Found {postsResult.Posts.Count} existing posts for user");
            }
            else
            {
                errors.Add($"Failed to fetch user posts: {postsResult.Message}");
            }

            // Step 3: Create new post if requested
            if (!string.IsNullOrEmpty(input.NewPostTitle))
            {
                logger.LogInformation("Creating new post: {PostTitle}", input.NewPostTitle);
                
                var createPostRequest = new CreatePostRequest
                {
                    UserId = input.UserId,
                    Title = input.NewPostTitle,
                    Body = input.NewPostBody ?? "Default post body created by NIWorker"
                };

                var createResult = await Workflow.ExecuteActivityAsync(
                    (ApiActivities act) => act.CreatePostAsync(createPostRequest),
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

                if (createResult.Success)
                {
                    results.Add($"Successfully created new post: '{createResult.Post?.Title}' (ID: {createResult.Post?.Id})");
                }
                else
                {
                    errors.Add($"Failed to create post: {createResult.Message}");
                }
            }

            // Step 4: Wait a moment and fetch updated posts
            if (!string.IsNullOrEmpty(input.NewPostTitle))
            {
                await Workflow.DelayAsync(TimeSpan.FromSeconds(1));
                
                logger.LogInformation("Fetching updated posts list...");
                var updatedPostsResult = await Workflow.ExecuteActivityAsync(
                    (ApiActivities act) => act.GetUserPostsAsync(input.UserId),
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

                if (updatedPostsResult.Success)
                {
                    results.Add($"Updated posts count: {updatedPostsResult.Posts.Count}");
                }
            }

            var overallSuccess = errors.Count == 0;
            logger.LogInformation("User management workflow completed for user {UserId}. Success: {Success}, Results: {ResultCount}, Errors: {ErrorCount}",
                input.UserId, overallSuccess, results.Count, errors.Count);

            return new UserManagementResult
            {
                Success = overallSuccess,
                UserId = input.UserId,
                UserName = userResult.User?.Name ?? "Unknown",
                Results = results,
                Errors = errors,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError("Workflow failed with exception: {Error}", ex.Message);
            errors.Add($"Workflow exception: {ex.Message}");
            
            return new UserManagementResult
            {
                Success = false,
                UserId = input.UserId,
                Results = results,
                Errors = errors,
                CompletedAt = DateTime.UtcNow
            };
        }
    }
}

public record UserManagementInput
{
    public int UserId { get; init; }
    public string? NewPostTitle { get; init; }
    public string? NewPostBody { get; init; }
}

public record UserManagementResult
{
    public bool Success { get; init; }
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public List<string> Results { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    public DateTime CompletedAt { get; init; }
}