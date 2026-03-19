namespace Shared.SeedWork
{
    public class ApiErrorResult : ApiResult
    {
        public ApiErrorResult(
            string? message = "An error occurred, Please try later",
            int statusCode = 400,
            string? details = null
        )
            : base(false, statusCode, message)
        {
            Details = details;
        }

        public ApiErrorResult(List<string> errors, int statusCode = 400)
            : base(false, statusCode)
        {
            Errors = errors;
            Message = "Multiple errors occurred.";
        }

        public List<string>? Errors { get; set; }
        public string? Details { get; set; }
    }
}
