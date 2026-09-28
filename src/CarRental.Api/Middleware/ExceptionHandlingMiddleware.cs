using CarRental.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace CarRental.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "произошла необработанна€ ошибка: {Message}", ex.Message);

                (HttpStatusCode statusCode, string message) = ex switch
                {
                    KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                    InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
                    ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
                    UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message),
                    DuplicateVinException => (HttpStatusCode.Conflict, ex.Message),
                    DuplicateUsernameException => (HttpStatusCode.Conflict, ex.Message),
                    UserNotEligibleException => (HttpStatusCode.Forbidden, ex.Message),
                    InsufficientDriverExperienceException => (HttpStatusCode.BadRequest, ex.Message),
                    _ => (HttpStatusCode.InternalServerError, "внутренн€€ ошибка сервера"),
                };

                context.Response.StatusCode = (int)statusCode;
                context.Response.ContentType = "application/json";

                var response = new { message };
                string json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
            }
        }
    }
}