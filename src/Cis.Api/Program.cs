using Cis.Api.Errors;
using Cis.Api.Health;
using Cis.Api.Configuration;
using Cis.Api.Middleware;
using Cis.Api.Security;
using Cis.Application;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Domain.Audit;
using Cis.Infrastructure;
using Cis.Infrastructure.Identity;
using Cis.Infrastructure.Operations;
using Cis.Infrastructure.Persistence;
using Cis.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

OperationalReadinessConfigurationValidator.Validate(builder.Configuration, builder.Environment.EnvironmentName);

var corsOptions = builder.Configuration.GetSection(ApiCorsOptions.SectionName).Get<ApiCorsOptions>() ?? new ApiCorsOptions();
var rateLimitingOptions = builder.Configuration.GetSection(SecurityRateLimitingOptions.SectionName).Get<SecurityRateLimitingOptions>() ?? new SecurityRateLimitingOptions();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        if (context.HttpContext.Request.Headers.TryGetValue(StandardHeaders.CorrelationId, out var correlationId))
        {
            context.ProblemDetails.Extensions["correlationId"] = correlationId.ToString();
        }
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        if (corsOptions.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsOptions.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Request rate limit exceeded. Retry later.",
            Type = "https://httpstatuses.com/429",
            Instance = context.HttpContext.Request.Path
        };
        problemDetails.Extensions["code"] = ApiErrorCodes.TooManyRequests;
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        var json = JsonConvert.SerializeObject(problemDetails);
        await context.HttpContext.Response.WriteAsync(json, cancellationToken);
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitingOptions.GlobalPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitingOptions.GlobalWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy(ApiRateLimitingPolicyNames.PublicAuth, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitingOptions.PublicAuthPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitingOptions.PublicAuthWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy(ApiRateLimitingPolicyNames.PublicEndpoint, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitingOptions.PublicEndpointPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitingOptions.PublicEndpointWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    await context.HttpContext.WriteSecurityAuditAsync("AuthenticationChallenged", AuditEventType.FailedLogin, "Unauthenticated request blocked by authentication middleware.");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/problem+json";
                    var pd = new ProblemDetails
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Title = "Unauthorized",
                        Detail = "Authentication is required to access this resource.",
                        Type = "https://httpstatuses.com/401",
                        Instance = context.HttpContext.Request.Path
                    };
                    pd.Extensions["code"] = ApiErrorCodes.Unauthorized;
                    pd.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                    if (context.HttpContext.Request.Headers.TryGetValue(StandardHeaders.CorrelationId, out var correlationId))
                    {
                        pd.Extensions["correlationId"] = correlationId.ToString();
                    }
                    var json = JsonConvert.SerializeObject(pd);
                    await context.Response.WriteAsync(json, context.HttpContext.RequestAborted);
                },
                OnForbidden = async context =>
                {
                    await context.HttpContext.WriteSecurityAuditAsync("AuthorizationForbidden", AuditEventType.Rejected, "Authenticated request blocked by authorization policy.");
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/problem+json";
                    var pd = new ProblemDetails
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Title = "Forbidden",
                        Detail = "The authenticated user does not have the required permission.",
                        Type = "https://httpstatuses.com/403",
                        Instance = context.HttpContext.Request.Path
                    };
                    pd.Extensions["code"] = ApiErrorCodes.Forbidden;
                    pd.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                    if (context.HttpContext.Request.Headers.TryGetValue(StandardHeaders.CorrelationId, out var correlationId))
                    {
                        pd.Extensions["correlationId"] = correlationId.ToString();
                    }
                    var json = JsonConvert.SerializeObject(pd);
                    await context.Response.WriteAsync(json, context.HttpContext.RequestAborted);
                }
        };
    });

builder.Services
    .AddControllers()
    .AddNewtonsoftJson()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Instance = context.HttpContext.Request.Path
            };
            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problemDetails);
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Victory CIS Management API",
        Version = "v1",
        Description = "Backend API for Victory Financial Services Ltd Collective Investment Scheme Management System."
    });
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT bearer token."
    });
    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    foreach (var permission in Permissions.AllKnown.Where(permission => permission != Permissions.All))
    {
        options.AddPolicy(PermissionPolicyName.For(permission), policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement(permission));
        });
    }

    options.AddPolicy(AuthorizationPolicyNames.CisUser, policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(AuthorizationPolicyNames.Maker, policy => policy.RequireAuthenticatedUser().RequireClaim("cis_role", "maker", "administrator"));
    options.AddPolicy(AuthorizationPolicyNames.Checker, policy => policy.RequireAuthenticatedUser().RequireClaim("cis_role", "checker", "administrator"));
    options.AddPolicy(AuthorizationPolicyNames.Approver, policy => policy.RequireAuthenticatedUser().RequireClaim("cis_role", "approver", "administrator"));
    options.AddPolicy(AuthorizationPolicyNames.Auditor, policy => policy.RequireAuthenticatedUser().RequireClaim("cis_role", "auditor", "administrator"));
    options.AddPolicy(AuthorizationPolicyNames.SystemAdministrator, policy => policy.RequireAuthenticatedUser().RequireClaim("cis_role", "administrator"));
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Application process is running."), tags: ["live"])
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready", "deep"])
    .AddCheck<StorageHealthCheck>("storage", tags: ["ready", "deep"])
    .AddCheck<BackgroundJobsHealthCheck>("background_jobs", tags: ["ready", "deep"])
    .AddDbContextCheck<CisDbContext>("postgresql", tags: ["ready", "deep"]);

var app = builder.Build();

if (app.Configuration.GetValue("Database:InitializeOnStartup", app.Environment.IsDevelopment()))
{
    await app.Services.InitializeDatabaseAsync(app.Configuration);
}

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecureHeadersMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("default");
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapGet("/api/v1/system/status", (HttpContext httpContext) =>
    {
        var status = new SystemStatusResponse("Victory CIS Management API", "v1", DateTime.UtcNow);
        return Results.Ok(ApiResponse<SystemStatusResponse>.Success(status, httpContext.TraceIdentifier));
    })
    .WithName("GetSystemStatus")
    .WithTags("System")
    .Produces<ApiResponse<SystemStatusResponse>>();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
})
.RequireRateLimiting(ApiRateLimitingPolicyNames.PublicEndpoint)
.AllowAnonymous();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
})
.RequireRateLimiting(ApiRateLimitingPolicyNames.PublicEndpoint)
.AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
})
.RequireRateLimiting(ApiRateLimitingPolicyNames.PublicEndpoint)
.AllowAnonymous();

app.MapGet("/metrics", () =>
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var lines = new[]
        {
            "# HELP cis_process_uptime_seconds Process uptime in seconds.",
            "# TYPE cis_process_uptime_seconds gauge",
            $"cis_process_uptime_seconds {decimal.Round((decimal)(DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds, 3)}",
            "# HELP cis_process_working_set_bytes Process working set.",
            "# TYPE cis_process_working_set_bytes gauge",
            $"cis_process_working_set_bytes {process.WorkingSet64}",
            "# HELP cis_thread_count Process thread count.",
            "# TYPE cis_thread_count gauge",
            $"cis_thread_count {process.Threads.Count}"
        };
        return Results.Text(string.Join('\n', lines), "text/plain");
    })
    .RequireAuthorization(policy => policy.RequireRole(RoleNames.SystemAdmin))
    .WithName("Metrics")
    .WithTags("Operations");

app.MapGet("/jobs", (IConfiguration configuration, IBackgroundJobDispatcher backgroundJobDispatcher, HttpContext httpContext) =>
    {
        var snapshot = backgroundJobDispatcher.GetSnapshot();
        var response = ApiResponse<BackgroundJobsDashboardResponse>.Success(new BackgroundJobsDashboardResponse(
            configuration["BackgroundJobs:Provider"] ?? "Quartz",
            configuration.GetValue("BackgroundJobs:DashboardEnabled", true),
            snapshot.QueuedCount,
            snapshot.ActiveCount,
            snapshot.CompletedCount,
            snapshot.FailedCount,
            snapshot.LastCompletedAtUtc,
            snapshot.LastFailedAtUtc,
            DateTime.UtcNow), httpContext.TraceIdentifier);
        return Results.Text(JsonConvert.SerializeObject(response), "application/json");
    })
    .RequireAuthorization(policy => policy.RequireRole(RoleNames.SystemAdmin))
    .WithName("BackgroundJobDashboard")
    .WithTags("Operations");

app.Run();

static string GetRateLimitPartitionKey(HttpContext httpContext)
{
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? httpContext.User.FindFirst("sub")?.Value;
    return !string.IsNullOrWhiteSpace(userId)
        ? $"user:{userId}"
        : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

public sealed record SystemStatusResponse(string Service, string Version, DateTime TimestampUtc);

public sealed record BackgroundJobsDashboardResponse(
    string Provider,
    bool DashboardEnabled,
    int QueuedCount,
    int ActiveCount,
    long CompletedCount,
    long FailedCount,
    DateTime? LastCompletedAtUtc,
    DateTime? LastFailedAtUtc,
    DateTime RetrievedAtUtc);

public partial class Program;
