namespace QLS.Backend.DTOs
{
    public class ApiResponse<T>
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Success(T data, string message = "Thành công")
        {
            return new ApiResponse<T>
            {
                Status = 200,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Error(int status, string message)
        {
            return new ApiResponse<T>
            {
                Status = status,
                Message = message
            };
        }
    }
}
