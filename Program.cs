using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using SatOps.Configuration;
using SatOps.Modules.Groundstation;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.Satellite;
using SatOps.Modules.User;
using SatOps.Authorization;
using System.Text.Json;
using SatOps.Data;
using Minio;
using Npgsql;
using System.Text;
using Serilog;
using dotenv.net;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using SatOps.Modules.GroundStationLink;

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

    // Add HttpClientFactory for calling external APIs (Auth0 UserInfo)
    builder.Services.AddHttpClient();

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

    // Prevent .NET from renaming JWT claims (e.g. "sub" → "nameidentifier")
    JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

    // Configure Auth0 JWT Bearer Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var auth0Settings = builder.Configuration.GetSection("Auth0");
            var domain = auth0Settings["Domain"] ?? throw new InvalidOperationException("Auth0 Domain not configured.");
            var audience = auth0Settings["Audience"] ?? throw new InvalidOperationException("Auth0 Audience not configured.");

            options.Authority = $"https://{domain}/";
            options.Audience = audience;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

            // Save the token so we can use it to call Auth0 UserInfo endpoint
            options.SaveToken = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            };

            // Enable BootstrapContext to access the raw token in ClaimsTransformer
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwtToken)
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            identity.BootstrapContext = jwtToken.RawData;
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

    // Authorization with role-based policies
    // Hierarchical roles: Admin (2) > Operator (1) > Viewer (0)
    builder.Services.AddAuthorization(options =>
    {
        // Special policy for ground station machine authentication
        options.AddPolicy(Policies.RequireGroundStation, policy =>
        {
            policy.RequireAuthenticatedUser()
                  .RequireClaim("type", "GroundStation");
        });

        // Role-based policies for human users
        // These use hierarchical role checking: higher roles include lower permissions
        options.AddPolicy(Policies.RequireViewer, policy =>
            policy.Requirements.Add(new MinimumRoleRequirement(SatOps.Modules.User.UserRole.Viewer)));

        options.AddPolicy(Policies.RequireOperator, policy =>
            policy.Requirements.Add(new MinimumRoleRequirement(SatOps.Modules.User.UserRole.Operator)));

        options.AddPolicy(Policies.RequireAdmin, policy =>
            policy.Requirements.Add(new MinimumRoleRequirement(SatOps.Modules.User.UserRole.Admin)));
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
    builder.Services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddSingleton<IWebSocketService, WebSocketService>();

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
    builder.Services.AddScoped<IObjectStorageService, ObjectStorageService>();
    builder.Services.AddScoped<ITelemetryService, TelemetryService>();
    builder.Services.AddScoped<IImageService, ImageService>();

    // Authorization handlers
    builder.Services.AddScoped<IAuthorizationHandler, MinimumRoleAuthorizationHandler>();
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