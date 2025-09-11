using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SatOps.Services.GroundStation;
using SatOps.Services;
using SatOps.Services.FlightPlan;
using SatOps.Controllers.FlightPlan;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Logging: emit structured JSON to console
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = false;
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<SatOpsDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseNetTopologySuite());
});

// DI
builder.Services.AddScoped<IGroundStationRepository, GroundStationRepository>();
builder.Services.AddScoped<IGroundStationService, GroundStationService>();
builder.Services.AddScoped<IFlightPlanRepository, FlightPlanRepository>();
builder.Services.AddScoped<IFlightPlanService, FlightPlanService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Global exception handling: never leak internals to clients
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");
        var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandler?.Error;

        // Always return sanitized ProblemDetails
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SatOpsDbContext>();
    db.Database.Migrate();
}

app.Run();
