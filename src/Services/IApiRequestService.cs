using System.Threading.Tasks;
using NIWorker.Models;

namespace NIWorker.Services
{
    public interface IApiRequestService
    {
        Task<ApiResponse<T>> ExecuteAsync<T>(string endpointName, object? parameters = null);
        Task<ApiResponse<string>> ExecuteAsync(string endpointName, object? parameters = null);
        Task<ApiResponse<T>> ExecuteCustomAsync<T>(ApiEndpoint endpoint, object? parameters = null);
    }

    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
    }
}