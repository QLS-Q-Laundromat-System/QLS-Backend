using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QLS.Backend.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // Try to retrieve user information from JWT claims
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? context.User?.FindFirst("id")?.Value 
                         ?? "Anonymous";

            var userRole = context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "Anonymous";

            // Push the UserId and UserName to Serilog's LogContext so all downstream logs include it
            using (Serilog.Context.LogContext.PushProperty("UserId", userId))
            using (Serilog.Context.LogContext.PushProperty("UserRole", userRole))
            {
                try
                {
                    await _next(context);

                    stopwatch.Stop();
                    var statusCode = context.Response.StatusCode;

                    // Log details of the successful or typical request
                    _logger.LogInformation(
                        "User {UserId} with role {UserRole} called {Method} {Path} - Response: {StatusCode} in {ElapsedMilliseconds}ms",
                        userId,
                        userRole,
                        context.Request.Method,
                        context.Request.Path,
                        statusCode,
                        stopwatch.ElapsedMilliseconds);
                }
                catch (System.Exception ex)
                {
                    stopwatch.Stop();

                    // Log exception request details
                    _logger.LogError(
                        ex,
                        "User {UserId} with role {UserRole} called {Method} {Path} failed with error after {ElapsedMilliseconds}ms",
                        userId,
                        userRole,
                        context.Request.Method,
                        context.Request.Path,
                        stopwatch.ElapsedMilliseconds);
                    throw;
                }
            }
        }
    }
}
