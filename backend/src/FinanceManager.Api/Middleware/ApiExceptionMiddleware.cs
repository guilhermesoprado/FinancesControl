using System.Net;
using System.Text.Json;
using FinanceManager.Application.Common.Exceptions;

namespace FinanceManager.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
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
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro nao tratado durante o processamento da requisicao.");
            await WriteErrorResponseAsync(context, exception);
        }
    }

    private static Task WriteErrorResponseAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            AppValidationException => HttpStatusCode.BadRequest,
            AppUnauthorizedException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(new
        {
            error = exception.Message,
            status = (int)statusCode
        });

        return context.Response.WriteAsync(payload);
    }
}
