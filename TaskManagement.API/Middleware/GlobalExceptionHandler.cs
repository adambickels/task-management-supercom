using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace TaskManagement.API.Middleware
{
    /// <summary>
    /// Global exception handling middleware
    /// </summary>
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
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
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var (statusCode, message) = exception switch
            {
                InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
                ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access"),
                _ => (HttpStatusCode.InternalServerError, "An internal server error occurred")
            };

            context.Response.StatusCode = (int)statusCode;

            var problemDetails = new
            {
                status = (int)statusCode,
                title = statusCode.ToString(),
                detail = message,
                instance = context.Request.Path,
                traceId = context.TraceIdentifier
            };

            var result = JsonSerializer.Serialize(problemDetails);
            return context.Response.WriteAsync(result);
        }
    }

    /// <summary>
    /// Extension method to register the global exception handler
    /// </summary>
    public static class GlobalExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandler>();
        }
    }
}
