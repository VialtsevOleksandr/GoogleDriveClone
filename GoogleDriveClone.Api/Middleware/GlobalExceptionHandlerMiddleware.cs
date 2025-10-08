using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;

namespace GoogleDriveClone.Api.Middleware;

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
            _logger.LogError(ex, "Сталася непередбачена помилка: {Message}", ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var errorResponse = new ApiErrorResponse(false, DomainErrors.General.UnexpectedError);
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}