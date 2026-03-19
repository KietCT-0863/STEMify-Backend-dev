namespace Shared.SeedWork
{
    public class ApiSuccessResult<T> : ApiResult
    {
        public ApiSuccessResult(T? data, int statusCode = 200, string message = "success")
            : base(true, statusCode, message)
        {
            Data = data;
        }

        public T? Data { get; set; }
    }
}
