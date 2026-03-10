using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PgSqlVectorSearch.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            NotImplementedException => (int)HttpStatusCode.NotImplemented,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var errorResponse = new
        {
            StatusCode = context.Response.StatusCode,
            Message = GetUserFriendlyMessage(exception),
            Detail = exception.Message,
            Timestamp = DateTime.UtcNow
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = context.Request.Host.Host.Contains("localhost")
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }

    private static string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => "Access denied. Please check your credentials.",
            ArgumentException => "Invalid request parameters.",
            KeyNotFoundException => "The requested resource was not found.",
            NotImplementedException => "This feature is not yet implemented.",
            _ => "An unexpected error occurred. Please try again later."
        };
    }
}