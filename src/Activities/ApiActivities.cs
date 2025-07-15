using Microsoft.Extensions.Logging;
using Temporalio.Activities;
using ApiRequestor.Services;

namespace NIWorker.Activities;

public class ApiActivities
{
    private readonly IApiRequestService _apiService;
    private readonly ILogger<ApiActivities> _logger;

    public ApiActivities(IApiRequestService apiService, ILogger<ApiActivities> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    [Activity]
    public async Task<UserApiResult> GetUserByIdAsync(int userId)
    {
        _logger.LogInformation("Fetching user with ID: {UserId}", userId);

        try
        {
            var response = await _apiService.ExecuteAsync<User>("GetUser", new { userId });

            if (response.IsSuccess && response.Data != null)
            {
                _logger.LogInformation("Successfully retrieved user: {UserName}", response.Data.Name);
                return new UserApiResult
                {
                    Success = true,
                    User = response.Data,
                    Message = "User retrieved successfully"
                };
            }
            else
            {
                _logger.LogWarning("Failed to retrieve user {UserId}: {Error}", userId, response.ErrorMessage);
                return new UserApiResult
                {
                    Success = false,
                    Message = response.ErrorMessage ?? "Unknown error occurred"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", userId);
            return new UserApiResult
            {
                Success = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    [Activity]
    public async Task<PostApiResult> CreatePostAsync(CreatePostRequest request)
    {
        _logger.LogInformation("Creating post for user {UserId}: {Title}", request.UserId, request.Title);

        try
        {
            // First, let's verify the user exists
            var userResponse = await _apiService.ExecuteAsync<User>("GetUser", new { userId = request.UserId });
            if (!userResponse.IsSuccess)
            {
                return new PostApiResult
                {
                    Success = false,
                    Message = $"User {request.UserId} not found"
                };
            }

            // Create the post
            var postData = new
            {
                title = request.Title,
                body = request.Body,
                userId = request.UserId
            };

            var response = await _apiService.ExecuteAsync<Post>("CreatePost", postData);

            if (response.IsSuccess && response.Data != null)
            {
                _logger.LogInformation("Successfully created post with ID: {PostId}", response.Data.Id);
                return new PostApiResult
                {
                    Success = true,
                    Post = response.Data,
                    Message = "Post created successfully"
                };
            }
            else
            {
                _logger.LogWarning("Failed to create post: {Error}", response.ErrorMessage);
                return new PostApiResult
                {
                    Success = false,
                    Message = response.ErrorMessage ?? "Unknown error occurred"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post for user {UserId}", request.UserId);
            return new PostApiResult
            {
                Success = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    [Activity]
    public async Task<PostListApiResult> GetUserPostsAsync(int userId)
    {
        _logger.LogInformation("Fetching posts for user {UserId}", userId);

        try
        {
            var response = await _apiService.ExecuteAsync<List<Post>>("GetPosts", new { userId = userId.ToString() });

            if (response.IsSuccess && response.Data != null)
            {
                _logger.LogInformation("Successfully retrieved {PostCount} posts for user {UserId}", 
                    response.Data.Count, userId);
                return new PostListApiResult
                {
                    Success = true,
                    Posts = response.Data,
                    Message = $"Retrieved {response.Data.Count} posts"
                };
            }
            else
            {
                _logger.LogWarning("Failed to retrieve posts for user {UserId}: {Error}", userId, response.ErrorMessage);
                return new PostListApiResult
                {
                    Success = false,
                    Posts = new List<Post>(),
                    Message = response.ErrorMessage ?? "Unknown error occurred"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving posts for user {UserId}", userId);
            return new PostListApiResult
            {
                Success = false,
                Posts = new List<Post>(),
                Message = $"Exception: {ex.Message}"
            };
        }
    }
}

// Data models for the JSONPlaceholder API
public record User
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Website { get; init; } = string.Empty;
}

public record Post
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}

// Request/Response models
public record CreatePostRequest
{
    public int UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}

public record UserApiResult
{
    public bool Success { get; init; }
    public User? User { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record PostApiResult
{
    public bool Success { get; init; }
    public Post? Post { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record PostListApiResult
{
    public bool Success { get; init; }
    public List<Post> Posts { get; init; } = new();
    public string Message { get; init; } = string.Empty;
}