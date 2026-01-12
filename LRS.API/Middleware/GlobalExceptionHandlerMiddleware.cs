using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace LRS.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IWebHostEnvironment env)
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
            _logger.LogError(ex, "An unhandled exception occurred. RequestPath: {RequestPath}", 
                context.Request.Path);
            await HandleExceptionAsync(context, ex, _env);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, IWebHostEnvironment env)
    {
        var code = HttpStatusCode.InternalServerError;
        string result;

        switch (exception)
        {
            case FileNotFoundException:
                code = HttpStatusCode.NotFound;
                result = JsonSerializer.Serialize(new
                {
                    message = exception.Message,
                    error = "Not Found",
                    details = env.IsDevelopment() ? exception.ToString() : null
                });
                break;
            case ArgumentNullException:
            case ArgumentException:
                code = HttpStatusCode.BadRequest;
                result = JsonSerializer.Serialize(new
                {
                    message = exception.Message,
                    error = "Bad Request",
                    details = env.IsDevelopment() ? exception.ToString() : null
                });
                break;
            case UnauthorizedAccessException:
                code = HttpStatusCode.Unauthorized;
                result = JsonSerializer.Serialize(new
                {
                    message = exception.Message,
                    error = "Unauthorized",
                    details = env.IsDevelopment() ? exception.ToString() : null
                });
                break;
            default:
                result = JsonSerializer.Serialize(new
                {
                    message = "An error occurred while processing your request.",
                    error = "Internal Server Error",
                    details = env.IsDevelopment() ? exception.ToString() : exception.Message
                });
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(result);
    }
}

