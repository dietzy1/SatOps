using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.FlightPlan.Commands;

namespace SatOps.Configuration
{
    public static class SwaggerConfiguration
    {
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSwaggerGen(options =>
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

                var domain = configuration["Auth0:Domain"];

                // OAuth2 security scheme configuration for Auth0
                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"https://{domain}/authorize"),
                            TokenUrl = new Uri($"https://{domain}/oauth/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                { "openid", "OpenID" },
                                { "profile", "Profile" },
                                { "email", "Email" }
                            }
                        }
                    }
                });

                // Keep Bearer token for backward compatibility or direct token usage
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
                                Id = "oauth2"
                            }
                        },
                        new[] { "openid", "profile", "email" }
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

                options.SchemaFilter<PolymorphicCommandSchemaFilter>();

                options.UseOneOfForPolymorphism();
                options.UseAllOfForInheritance();

                options.SchemaFilter<CommandExamplesSchemaFilter>();
            });

            return services;
        }

        // Helper method to determine if a controller is internal
        private static bool IsInternalController(string? controllerName)
        {
            var internalControllers = new[] { "GroundStationLink" };
            return controllerName != null && internalControllers.Contains(controllerName);
        }

        // Extension method for Swagger UI configuration
        public static WebApplication UseSwaggerConfiguration(this WebApplication app, IConfiguration configuration)
        {
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

                    // Auth0 OAuth configuration for Swagger UI
                    c.OAuthClientId(configuration["Auth0:ClientId"]);
                    c.OAuthClientSecret(configuration["Auth0:ClientSecret"]);
                    c.OAuthScopes("openid", "profile", "email");
                    c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>
                    {
                        { "audience", configuration["Auth0:Audience"] ?? "" }
                    });
                });
            }

            return app;
        }
    }
    /// <summary>
    /// Ensures Swagger generates proper discriminator for polymorphic commands
    /// </summary>
    public class PolymorphicCommandSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(Command))
            {
                schema.Discriminator = new OpenApiDiscriminator
                {
                    PropertyName = "commandType",
                    Mapping = new Dictionary<string, string>
                    {
                        [CommandTypeConstants.TriggerCapture] = "#/components/schemas/TriggerCaptureCommand",
                        [CommandTypeConstants.TriggerPipeline] = "#/components/schemas/TriggerPipelineCommand"
                    }
                };
            }
        }
    }

    /// <summary>
    /// Adds example request bodies for each command type
    /// </summary>
    public class CommandExamplesSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(TriggerCaptureCommand))
            {
                schema.Example = new OpenApiObject
                {
                    ["commandType"] = new OpenApiString(CommandTypeConstants.TriggerCapture),
                    ["captureLocation"] = new OpenApiObject
                    {
                        ["latitude"] = new OpenApiDouble(55.6761),  // Copenhagen
                        ["longitude"] = new OpenApiDouble(12.5683)
                    },
                    ["cameraSettings"] = new OpenApiObject
                    {
                        ["cameraId"] = new OpenApiString("1800 U-500c"),
                        ["type"] = new OpenApiInteger(0),
                        ["exposureMicroseconds"] = new OpenApiInteger(55000),
                        ["iso"] = new OpenApiDouble(1.0),
                        ["numImages"] = new OpenApiInteger(5),
                        ["intervalMicroseconds"] = new OpenApiInteger(1000000),
                        ["observationId"] = new OpenApiInteger(1),
                        ["pipelineId"] = new OpenApiInteger(1)
                    },
                    ["maxOffNadirDegrees"] = new OpenApiDouble(10.0),
                    ["maxSearchDurationHours"] = new OpenApiInteger(48)
                };
            }
            else if (context.Type == typeof(TriggerPipelineCommand))
            {
                schema.Example = new OpenApiObject
                {
                    ["commandType"] = new OpenApiString(CommandTypeConstants.TriggerPipeline),
                    ["executionTime"] = new OpenApiString("2025-10-10T12:05:00Z"),
                    ["mode"] = new OpenApiInteger(1)
                };
            }
            else if (context.Type == typeof(CreateFlightPlanDto))
            {
                schema.Example = new OpenApiObject
                {
                    ["gsId"] = new OpenApiInteger(1),
                    ["satId"] = new OpenApiInteger(1),
                    ["name"] = new OpenApiString("Mission Alpha Observation"),
                    ["commands"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["commandType"] = new OpenApiString(CommandTypeConstants.TriggerCapture),
                            ["captureLocation"] = new OpenApiObject
                            {
                                ["latitude"] = new OpenApiDouble(55.6761),  // Copenhagen
                                ["longitude"] = new OpenApiDouble(12.5683)
                            },
                            ["cameraSettings"] = new OpenApiObject
                            {
                                ["cameraId"] = new OpenApiString("1800 U-500c"),
                                ["type"] = new OpenApiInteger(0),
                                ["exposureMicroseconds"] = new OpenApiInteger(55000),
                                ["iso"] = new OpenApiDouble(1.0),
                                ["numImages"] = new OpenApiInteger(5),
                                ["intervalMicroseconds"] = new OpenApiInteger(1000000),
                                ["observationId"] = new OpenApiInteger(1),
                                ["pipelineId"] = new OpenApiInteger(1)
                            },
                            ["maxOffNadirDegrees"] = new OpenApiDouble(10.0),
                            ["maxSearchDurationHours"] = new OpenApiInteger(48)
                        },
                        new OpenApiObject
                        {
                            ["commandType"] = new OpenApiString(CommandTypeConstants.TriggerPipeline),
                            ["executionTime"] = new OpenApiString("2025-10-10T12:05:00Z"),
                            ["mode"] = new OpenApiInteger(1)
                        }
                    }
                };
            }
        }
    }
}