using Microsoft.EntityFrameworkCore;
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
using SatOps.Data;
using Minio;
using System.Text;
using SatOps.Modules.Auth;
using SatOps.Configuration;
using System.Text.Json.Serialization.Metadata;

var builder = WebApplication.CreateBuilder(args);

// Logging: emit structured JSON to console
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = false;
});

builder.Services.AddControllers()
 .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;

        // CRITICAL: Add this for polymorphic Command deserialization
        options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();

        // Keep your enum converter
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(
                JsonNamingPolicy.SnakeCaseUpper
            )
        );
    });
/*  .AddJsonOptions(options =>
 {
     // Configure System.Text.Json for polymorphic serialization
     options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
     options.JsonSerializerOptions.Converters.Add(
         new System.Text.Json.Serialization.JsonStringEnumConverter(
             JsonNamingPolicy.SnakeCaseUpper
         )
     );
 }); */

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
builder.Services.AddDbContext<SatOpsDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("SatOps"));

    // disable verbose logging of sensitive data
    options.EnableSensitiveDataLogging(false);
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

// Global exception handling: never leak internals to clients
app.UseGlobalExceptionHandler();

app.UseSwaggerConfiguration();

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