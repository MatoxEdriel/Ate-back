namespace Ate.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully.")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = null,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public static ApiResponse<T> FailureResponse(string message, IReadOnlyList<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors ?? Array.Empty<string>(),
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}

public class ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string>? Errors { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public static ApiResponse SuccessResponse(string message = "Operation completed successfully.")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Errors = null,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public static ApiResponse FailureResponse(string message, IReadOnlyList<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors ?? Array.Empty<string>(),
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
