namespace Shared.SeedWork
{
    public class ApiResult
    {
        public ApiResult() { }

        public ApiResult(bool isSucceeded, int statusCode = 200, string? message = null)
        {
            Message = message;
            IsSucceeded = isSucceeded;
            StatusCode = statusCode;
        }

        public bool IsSucceeded { get; set; }
        public string? Message { get; set; }
        public int StatusCode { get; set; }

        // Static factory methods for common scenarios
        public static ApiResult Success(
            string message = "Operation completed successfully",
            int statusCode = 200
        ) => new(true, statusCode, message);

        public static ApiResult Failed(string message = "Operation failed", int statusCode = 400) =>
            new(false, statusCode, message);

        public static ApiResult<T> Succeeded<T>(
            T data,
            string message = "Operation completed successfully",
            int statusCode = 200
        ) => new(data, true, statusCode, message);

        public static ApiResult<T> Failed<T>(
            string message = "Operation failed",
            int statusCode = 400
        ) => new(default(T), false, statusCode, message);

        public static ApiResult<T> Failed<T>(List<string> errors, int statusCode = 400) =>
            new(default(T), false, statusCode, "Multiple errors occurred", errors);
    }

    public class ApiResult<T> : ApiResult
    {
        public ApiResult() { }

        public ApiResult(
            T? data,
            bool isSucceeded,
            int statusCode = 200,
            string? message = null,
            List<string>? errors = null
        )
            : base(isSucceeded, statusCode, message)
        {
            Data = data;
            Errors = errors;
        }

        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        // Static factory methods for generic results
        public static ApiResult<T> Succeeded(
            T data,
            string message = "Operation completed successfully",
            int statusCode = 200
        ) => new(data, true, statusCode, message);

        public static ApiResult<T> Failed(
            string message = "Operation failed",
            int statusCode = 400
        ) => new(default(T), false, statusCode, message);

        public static ApiResult<T> Failed(List<string> errors, int statusCode = 400) =>
            new(default(T), false, statusCode, "Multiple errors occurred", errors);
    }
}
