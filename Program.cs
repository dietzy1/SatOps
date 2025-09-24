using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using SatOps.Modules.Groundstation;
using SatOps.Modules.Schedule;
using SatOps.Modules.Satellite;
using SatOps.Modules.User;
using SatOps.Modules.Groundstation.Health;
using SatOps.Authorization;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using SatOps.Data;

var builder = WebApplication.CreateBuilder(args);

// Logging: emit structured JSON to console
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = false;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "SatOps API",
        Description = @"
A comprehensive **ASP.NET Core Web API** for managing satellite operations including:

- 🛰️ Satellite tracking and monitoring
- 📡 Communication scheduling  
- 🔧 Maintenance operations
- 📊 Telemetry data processing

## Features
- Real-time satellite status updates
- Automated orbit calculations
- Mission planning tools
- Integration with ground stations
- Role-Based Access Control (RBAC) with scope and role-based permissions
        ".Trim()
    });
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Database
builder.Services.AddDbContext<SatOpsDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseNetTopologySuite());
});

var wayfAuthority = builder.Configuration["WAYF:Authority"] ?? "https://wayf.wayf.dk";
var wayfAudience = builder.Configuration["WAYF:Audience"] ?? "your-client-id"; // Get from WAYF secretariat
var wayfIssuer = builder.Configuration["WAYF:Issuer"] ?? "https://wayf.wayf.dk";

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = wayfAuthority;
        options.RequireHttpsMetadata = true;
        options.MetadataAddress = $"{wayfAuthority}/oidc/config/.well-known/openid-configuration";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = wayfIssuer,
            ValidAudience = wayfAudience,
        };
    });



// Authorization with custom policies
builder.Services.AddAuthorization(options =>
{
    // Scope-based policies
    options.AddPolicy("ReadGroundStations", policy =>
        policy.Requirements.Add(new ScopeRequirement("read:ground-stations")));
    options.AddPolicy("WriteGroundStations", policy =>
        policy.Requirements.Add(new ScopeRequirement("write:ground-stations")));
    options.AddPolicy("DeleteGroundStations", policy =>
        policy.Requirements.Add(new ScopeRequirement("delete:ground-stations")));

    options.AddPolicy("ReadSatellites", policy =>
        policy.Requirements.Add(new ScopeRequirement("read:satellites")));
    options.AddPolicy("WriteSatellites", policy =>
        policy.Requirements.Add(new ScopeRequirement("write:satellites")));
    options.AddPolicy("DeleteSatellites", policy =>
        policy.Requirements.Add(new ScopeRequirement("delete:satellites")));

    options.AddPolicy("ReadFlightPlans", policy =>
        policy.Requirements.Add(new ScopeRequirement("read:flight-plans")));
    options.AddPolicy("WriteFlightPlans", policy =>
        policy.Requirements.Add(new ScopeRequirement("write:flight-plans")));
    options.AddPolicy("DeleteFlightPlans", policy =>
        policy.Requirements.Add(new ScopeRequirement("delete:flight-plans")));
    options.AddPolicy("ApproveFlightPlans", policy =>
        policy.Requirements.Add(new ScopeRequirement("approve:flight-plans")));

    options.AddPolicy("ManageUsers", policy =>
        policy.Requirements.Add(new ScopeRequirement("manage:users")));

    // Role-based policies
    options.AddPolicy("RequireViewer", policy =>
        policy.Requirements.Add(new RoleRequirement("Viewer")));
    options.AddPolicy("RequireOperator", policy =>
        policy.Requirements.Add(new RoleRequirement("Operator")));
    options.AddPolicy("RequireAdmin", policy =>
        policy.Requirements.Add(new RoleRequirement("Admin")));
});

// DI
builder.Services.AddScoped<IGroundStationRepository, GroundStationRepository>();
builder.Services.AddScoped<IGroundStationService, GroundStationService>();
builder.Services.AddScoped<IFlightPlanRepository, FlightPlanRepository>();
builder.Services.AddScoped<IFlightPlanService, FlightPlanService>();
builder.Services.AddScoped<ISatelliteRepository, SatelliteRepository>();
builder.Services.AddScoped<ISatelliteService, SatelliteService>();
builder.Services.AddScoped<ICelestrackClient, CelestrackClient>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// Authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, ScopeAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();

// Health check services
builder.Services.AddHttpClient<IGroundStationHealthService, GroundStationHealthService>();
builder.Services.AddHostedService<GroundStationHealthCheckService>();

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
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SatOpsDbContext>();
    db.Database.Migrate();
}

app.Run();