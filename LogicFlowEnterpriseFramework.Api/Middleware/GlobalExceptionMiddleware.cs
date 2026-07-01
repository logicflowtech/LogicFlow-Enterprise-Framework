using System.Net;
using FluentValidation;
using LogicFlowEnterpriseFramework.Shared.Responses;

namespace LogicFlowEnterpriseFramework.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception");
            await WriteErrorAsync(context, exception);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            ValidationException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        IReadOnlyDictionary<string, string[]>? errors = exception is ValidationException validationException
            ? validationException.Errors.GroupBy(x => x.PropertyName).ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).ToArray())
            : null;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        var message = GetSafeMessage(exception, statusCode, environment.IsDevelopment());
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Failure(message, errors));
    }

    private static string GetSafeMessage(Exception exception, HttpStatusCode statusCode, bool includeDetails)
    {
        if (includeDetails || statusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            return exception.Message;
        }

        return "An unexpected error occurred.";
    }
}
