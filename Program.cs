using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using SatOps.Modules.Auth;
using SatOps.Configuration;
using SatOps.Modules.Groundstation;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.Satellite;
using SatOps.Modules.User;
using SatOps.Modules.Operation;
using SatOps.Modules.Gateway;
using SatOps.Authorization;
using System.Text.Json;
using SatOps.Data;
using Minio;
using Npgsql;
using System.Text;
using Serilog;
using dotenv.net;

DotEnv.Load();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
try
{
    Log.Information("Starting SatOps web host");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerConfiguration();

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
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(
        builder.Configuration.GetConnectionString("DefaultConnection")!
    );

    // Enable dynamic JSON serialization support (for List<string>, etc.)
    dataSourceBuilder.EnableDynamicJson();

    var dataSource = dataSourceBuilder.Build();

    builder.Services.AddDbContext<SatOpsDbContext>(options =>
    {
        options.UseNpgsql(dataSource, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("SatOps");
        });
    });

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured.");

            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
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
        // Special policy for ground station authentication
        options.AddPolicy(Policies.RequireGroundStation, policy =>
        {
            policy.RequireAuthenticatedUser()
                  .RequireClaim("type", "GroundStation");
        });

        // Admin policy - requires Admin role
        options.AddPolicy(Policies.RequireAdmin, policy =>
        {
            policy.RequireAuthenticatedUser()
                  .RequireRole("Admin");
        });

        // Ground Station scope-based policies
        options.AddPolicy(Policies.ReadGroundStations, policy =>
            policy.Requirements.Add(new ScopeRequirement(Scopes.ReadGroundStations)));
        options.AddPolicy(Policies.WriteGroundStations, policy =>
            policy.Requirements.Add(new ScopeRequirement(Scopes.WriteGroundStations)));

        // Satellite scope-based policies
        options.AddPolicy(Policies.ReadSatellites, policy =>
            policy.Requirements.Add(new ScopeRequirement(Scopes.ReadSatellites)));
        options.AddPolicy(Policies.WriteSatellites, policy =>
            policy.Requirements.Add(new ScopeRequirement(Scopes.WriteSatellites)));

        // Flight Plan scope-based policies
        options.AddPolicy(Policies.ReadFlightPlans, policy =>
            policy.Requirements.Add(new ScopeRequirement(Scopes.ReadFlightPlans)));
        options.AddPolicy(Policies.WriteFlightPlans, policy =>
            policy.Requirements.Add(new ScopeRequirement(Scopes.WriteFlightPlans)));

        // User management scope-based policy
        options.AddPolicy(Policies.ManageUsers, policy =>
            policy.Requirements.Add(new ScopeRequirement(Scopes.ManageUsers)));

        // Ground Station operation policies (limited to ground stations only)
        options.AddPolicy(Policies.UploadTelemetry, policy =>
            policy.Requirements.Add(new ScopeRequirement(Scopes.UploadTelemetry)));
        options.AddPolicy(Policies.UploadImages, policy =>
            policy.Requirements.Add(new ScopeRequirement(Scopes.UploadImages)));
        options.AddPolicy(Policies.EstablishWebSocket, policy =>
            policy.Requirements.Add(new ScopeRequirement(Scopes.EstablishWebSocket)));
    });

    // DI
    builder.Services.AddScoped<IGroundStationRepository, GroundStationRepository>();
    builder.Services.AddScoped<IGroundStationService, GroundStationService>();
    builder.Services.AddScoped<IFlightPlanRepository, FlightPlanRepository>();
    builder.Services.AddScoped<IFlightPlanService, FlightPlanService>();
    builder.Services.AddScoped<IImagingCalculation, ImagingCalculation>();
    builder.Services.AddScoped<ISatelliteRepository, SatelliteRepository>();
    builder.Services.AddScoped<ISatelliteService, SatelliteService>();
    builder.Services.AddScoped<ICelestrackClient, CelestrackClient>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<SatOps.Modules.Overpass.IOverpassRepository, SatOps.Modules.Overpass.OverpassRepository>();
    builder.Services.AddScoped<SatOps.Modules.Overpass.IOverpassService, SatOps.Modules.Overpass.OverpassService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddSingleton<IGroundStationGatewayService, GroundStationGatewayService>();

    // MinIO Configuration
    builder.Services.AddSingleton(sp =>
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

    // Background services
    builder.Services.AddHostedService<GroundStationHealthCheckWorker>();
    builder.Services.AddHostedService<SchedulerService>();


    var app = builder.Build();

    app.UseSerilogRequestLogging();

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

    app.UseSwaggerConfiguration();

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Apply pending migrations on startup
    if (!AppDomain.CurrentDomain.FriendlyName.Equals("ef", StringComparison.OrdinalIgnoreCase))
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SatOpsDbContext>();
            db.Database.Migrate();
        }
    }

    app.UseWebSockets();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}