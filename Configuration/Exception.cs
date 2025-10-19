using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SatOps.Configuration;

public static class ExceptionHandlerConfiguration
{
    public static void UseGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");
                var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionHandler?.Error;

                // Always return sanitized ProblemDetails - never leak stack traces to clients
                var problem = new ProblemDetails
                {
                    Title = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "about:blank",
                    Detail = null,
                    Instance = context.Request.Path
                };

                // Log full exception details for diagnostics
                if (exception != null)
                {
                    logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                var json = JsonSerializer.Serialize(problem);
                await context.Response.WriteAsync(json);
            });
        });
    }
}
