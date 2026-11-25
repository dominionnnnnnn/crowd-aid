using CrowdAid.Data.DTOs.Common;
using CrowdAid.Data.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace CrowdAid.Api.Middleware;

/// <summary>Turns exceptions into the spec's error shape. Controllers just throw.</summary>
public class AppExceptionHandler(ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, body) = ex switch
        {
            ValidationFailedException v => (v.StatusCode, ErrorResponse.Of(v.Code, v.Message, v.Fields)),
            AppException a => (a.StatusCode, ErrorResponse.Of(a.Code, a.Message)),
            UnauthorizedAccessException => (401, ErrorResponse.Of("UNAUTHENTICATED", "Please sign in.")),
            _ => (500, ErrorResponse.Of("SERVER_ERROR", "Something went wrong. Please try again.")),
        };

        if (status >= 500)
            logger.LogError(ex, "Unhandled error on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(body, ct);
        return true;
    }
}
