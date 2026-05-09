using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using QLS.Backend.DTOs;
using QLS.Backend.Exceptions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLS.Backend.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đã xảy ra lỗi tại {Path}: {Message}", context.Request.Path, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = (int)HttpStatusCode.InternalServerError;
            var message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";

            // Xác định StatusCode và Message dựa trên loại Exception
            switch (exception)
            {
                case ApiException apiEx:
                    statusCode = apiEx.StatusCode;
                    message = apiEx.Message;
                    break;
                case ArgumentNullException _:
                case ArgumentException _:
                case InvalidOperationException _:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;
                case UnauthorizedAccessException _:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    message = "Bạn không có quyền thực hiện hành động này.";
                    break;
                case KeyNotFoundException _:
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Không tìm thấy dữ liệu yêu cầu.";
                    break;
                default:
                    // Với lỗi hệ thống không xác định, chỉ hiển thị message thật sự khi ở Development
                    if (_env.IsDevelopment())
                    {
                        message = exception.Message;
                    }
                    break;
            }

            context.Response.StatusCode = statusCode;

            // Tạo response chuẩn hóa
            var response = ApiResponse<object>.Error(statusCode, message);

            // Nếu đang ở môi trường Dev, thêm StackTrace để dễ debug
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            
            object finalResponse = response;
            if (_env.IsDevelopment())
            {
                finalResponse = new
                {
                    response.Status,
                    response.Message,
                    Detail = exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                };
            }

            var result = JsonSerializer.Serialize(finalResponse, jsonOptions);
            await context.Response.WriteAsync(result);
        }
    }
}
