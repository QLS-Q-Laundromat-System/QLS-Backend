using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using QLS.Backend.DTOs;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLS.Backend.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đã xảy ra lỗi hệ thống: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            int statusCode;
            string message;

            switch (exception)
            {
                case ArgumentException e:
                    statusCode = StatusCodes.Status400BadRequest;
                    message = e.Message;
                    break;
                case UnauthorizedAccessException e:
                    statusCode = StatusCodes.Status401Unauthorized;
                    message = e.Message;
                    break;
                case KeyNotFoundException e:
                    statusCode = StatusCodes.Status404NotFound;
                    message = e.Message;
                    break;
                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    message = "Lỗi hệ thống: " + exception.Message;
                    break;
            }

            context.Response.StatusCode = statusCode;

            var response = ApiResponse<object>.Error(statusCode, message);
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            return context.Response.WriteAsync(json);
        }
    }
}
