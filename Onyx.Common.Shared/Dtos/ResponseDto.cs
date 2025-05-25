namespace Onyx.Common.Shared.Dtos
{
    public class ResponseDto<T>
    {
        public T? Result { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string Message { get; set; } = string.Empty;

        public static ResponseDto<T> Success(T result, string message = "")
        {
            return new ResponseDto<T> { IsSuccess = true, Result = result, Message = message };
        }
    }
}