using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using SatOps.Modules.Groundstation;
using SatOps.Modules.Schedule;
using SatOps.Modules.Satellite;
using SatOps.Modules.User;
using SatOps.Modules.Groundstation.Health;
using SatOps.Modules.Operation;
using SatOps.Authorization;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using SatOps.Data;
using Minio;
using System.Text;
using SatOps.Modules.Auth;

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
    // Public API documentation
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "SatOps Public API",
        Description = @"
A comprehensive **ASP.NET Core Web API** for managing satellite operations including:

- 🛰️ Satellite tracking and monitoring
- 📡 Communication scheduling  
- 🔧 Maintenance operations
- 📊 Data access and reporting

## Features
- Real-time satellite status updates
- Automated orbit calculations
- Mission planning tools
- Role-Based Access Control (RBAC) with scope and role-based permissions

**Note**: This is the public-facing API. Internal operations are available on a separate endpoint.
        ".Trim()
    });

    // Internal API documentation
    options.SwaggerDoc("internal", new OpenApiInfo
    {
        Version = "v1",
        Title = "SatOps Internal API",
        Description = @"
**Internal Operations API** for satellite communications and data processing:

- 📤 Command transmission to satellites
- 📥 Telemetry data reception from satellites
- 🖼️ Image data reception and processing
- 🔄 Real-time operational status updates

## Features
- Command lifecycle management (Pending → Sent → Acknowledged)
- Large file handling for telemetry and images
- MinIO object storage integration
- Ground station communication endpoints

**Note**: These endpoints are intended for internal ground station operations and satellite communications.
        ".Trim()
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Configure which controllers belong to which API
    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];

        return docName switch
        {
            "v1" => !IsInternalController(controllerName),
            "internal" => IsInternalController(controllerName),
            _ => false
        };
    });
});

// Helper method to determine if a controller is internal
static bool IsInternalController(string? controllerName)
{
    var internalControllers = new[] { "Operations", "Auth" };
    return controllerName != null && internalControllers.Contains(controllerName);
}

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
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("SatOps"));
});

var wayfAuthority = builder.Configuration["WAYF:Authority"] ?? "https://wayf.wayf.dk";
var wayfAudience = builder.Configuration["WAYF:Audience"] ?? "your-client-id"; // Get from WAYF secretariat
var wayfIssuer = builder.Configuration["WAYF:Issuer"] ?? "https://wayf.wayf.dk";

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => // Scheme 1: For Users (WAYF)
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
    })
    .AddJwtBearer(AuthConstants.GroundStationAuthScheme, options => // Scheme 2: For Ground Stations
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });



// Authorization with custom policies
builder.Services.AddAuthorization(options =>
{
    // Policy for ground station authentication
    options.AddPolicy("RequireGroundStation", policy =>
    {
        policy.AddAuthenticationSchemes(AuthConstants.GroundStationAuthScheme)
              .RequireAuthenticatedUser()
              .RequireClaim("type", "GroundStation");
    });
    // Scope-based policies for user permissions
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
builder.Services.AddScoped<SatOps.Modules.Overpass.IOverpassRepository, SatOps.Modules.Overpass.OverpassRepository>();
builder.Services.AddScoped<SatOps.Modules.Overpass.IService, SatOps.Modules.Overpass.Service>();
builder.Services.AddScoped<IAuthService, AuthService>();

// MinIO Configuration
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var endpoint = configuration.GetValue<string>("MinIO:Endpoint") ?? "localhost:9000";
    var accessKey = configuration.GetValue<string>("MinIO:AccessKey") ?? "minioadmin";
    var secretKey = configuration.GetValue<string>("MinIO:SecretKey") ?? "minioadmin";
    var secure = configuration.GetValue<bool>("MinIO:Secure");

    return new MinioClient()
        .WithEndpoint(endpoint)
        .WithCredentials(accessKey, secretKey)
        .WithSSL(secure)
        .Build();
});

// Operation Services
builder.Services.AddScoped<IMinioService, MinioService>();
builder.Services.AddScoped<ITelemetryService, TelemetryService>();
builder.Services.AddScoped<IImageService, ImageService>();

// Authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, ScopeAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
builder.Services.AddScoped<IClaimsTransformation, UserPermissionsClaimsTransformation>();


// Health check services
builder.Services.AddHttpClient<IGroundStationHealthService, GroundStationHealthService>();
builder.Services.AddHostedService<GroundStationHealthCheckWorker>();

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
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SatOps Public API v1");
        c.SwaggerEndpoint("/swagger/internal/swagger.json", "SatOps Internal API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "SatOps API Documentation";
        c.DefaultModelsExpandDepth(-1); // Hide schemas section by default
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
    });
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

public static class AuthConstants
{
    public const string GroundStationAuthScheme = "GroundStation_Bearer";
}