using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WestCoastFitness.Application;

namespace WestCoastFitness.Api;

/// <summary>Maps business-rule failures to problem responses and hides internal error details.</summary>
public sealed partial class ClubExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public ClubExceptionMiddleware(RequestDelegate next, ILogger<ClubExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            await _next(context);
        }
        catch (ClubException ex)
        {
            await WriteAsync(context, StatusFor(ex.Kind), ex.Message);
        }
        catch (DbUpdateException ex)
        {
            LogConstraintViolation(ex);
            await WriteAsync(context, StatusCodes.Status409Conflict, "The request conflicts with existing data.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogUnhandled(ex, context.Request.Path.Value);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "A database constraint rejected the request.")]
    private partial void LogConstraintViolation(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing {Path}.")]
    private partial void LogUnhandled(Exception exception, string? path);

    private static int StatusFor(ClubErrorKind kind) => kind switch
    {
        ClubErrorKind.Validation => StatusCodes.Status400BadRequest,
        ClubErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ClubErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ClubErrorKind.NotFound => StatusCodes.Status404NotFound,
        ClubErrorKind.PaymentDeclined => StatusCodes.Status402PaymentRequired,
        _ => StatusCodes.Status409Conflict,
    };

    private static Task WriteAsync(HttpContext context, int status, string detail)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Detail = detail });
    }
}
