using System.Collections.Generic;

namespace NIWorker.Models
{
    public class ApiConfiguration
    {
        public string BaseUrl { get; set; } = string.Empty;
        public Dictionary<string, string> DefaultHeaders { get; set; } = new();
        public int TimeoutSeconds { get; set; } = 30;
        public List<ApiEndpoint> Endpoints { get; set; } = new();
        public AuthenticationConfig? Authentication { get; set; }
    }

    public class ApiEndpoint
    {
        public string Name { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public string Path { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
        public Dictionary<string, object> QueryParameters { get; set; } = new();
        public object? Body { get; set; }
        public string ContentType { get; set; } = "application/json";
    }

    public class AuthenticationConfig
    {
        public string Type { get; set; } = string.Empty; // "Bearer", "ApiKey", "Basic"
        public string Token { get; set; } = string.Empty;
        public string HeaderName { get; set; } = "Authorization";
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}