using System;

namespace QLS.Backend.Exceptions
{
    /// <summary>
    /// Custom exception dùng để ném ra các lỗi nghiệp vụ (Business Logic) hoặc lỗi API xác định.
    /// Cho phép chỉ định StatusCode để Middleware có thể bắt và trả về đúng mã lỗi cho Frontend.
    /// </summary>
    public class ApiException : Exception
    {
        public int StatusCode { get; }

        public ApiException(string message, int statusCode = 400) : base(message)
        {
            StatusCode = statusCode;
        }

        public ApiException(string message, Exception innerException, int statusCode = 400) 
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
