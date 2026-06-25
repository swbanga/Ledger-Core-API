using System.Text;
using LedgerCore.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using LedgerCore.Infrastructure.Authentication;
using LedgerCore.Application.Authentication;
using LedgerCore.Application.Interfaces;
using LedgerCore.Infrastructure.Idempotency;
using LedgerCore.Application;
using LedgerCore.Application.Contracts;
using LedgerCore.Api.Services;
using LedgerCore.Infrastructure;
using Serilog;
using Serilog.Extensions.Hosting;
using Serilog.Formatting.Compact;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

Serilog.Log.Logger = new Serilog.LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter())
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console());

// Add services to the container.
// Add services to the container.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "LedgerCore API";
        document.Info.Version = "v1.0";
        document.Info.Description = "Enterprise Financial Ledger Project.\n\n" +
                                    "> **DevSecOps Notice:** This interactive intelligence layer (Scalar UI) is deliberately exposed in this cloud pilot for public testing and architectural evaluation by technical recruiters. In a live banking environment, this interactive footprint would be aggressively amputated or restricted behind an internal API Gateway and corporate VPN to neutralize the application's public attack surface.\n\n" +
                                    "**Engineered by Super Washington Banga | Backend and Cloud Systems Engineering**\n\n" +
                                    "github.com/swbanga | linkedin.com/swbanga";
        return Task.CompletedTask;
    });
});
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestContext, RequestContext>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddSingleton<IJwtProvider, JwtProvider>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
builder.Services.AddTransient<IIdempotencyService, RedisIdempotencyService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddEntityFrameworkCoreInstrumentation();
        tracing.AddSource("MassTransit");
        tracing.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
    });

builder.Services.AddApplication();

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseExceptionHandler();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);

        var requestContext = httpContext.RequestServices.GetService<IRequestContext>();
        if (requestContext != null)
        {
            diagnosticContext.Set("UserId", requestContext.GetUserId().ToString());
            diagnosticContext.Set("IpAddress", requestContext.GetIpAddress());
            diagnosticContext.Set("DeviceId", requestContext.GetDeviceId());
        }

        var activity = System.Diagnostics.Activity.Current;
        if (activity != null)
        {
            diagnosticContext.Set("TraceId", activity.TraceId.ToString());
            diagnosticContext.Set("SpanId", activity.SpanId.ToString());
        }
    };
});

// Middleware & Security Chain
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// 1. Core Endpoint Routing Map
app.MapControllers();

// 2. OpenAPI Generation & Scalar UI Exposure (Enabled for Cloud Pilot)
app.MapOpenApi();
app.MapScalarApiReference();

// 3. Root URL Redirect Hijack to Default Landing Page
app.MapGet("/", () => Results.Redirect("/scalar/v1"))
   .ExcludeFromDescription();

// 4. Infrastructure Health Probes
app.UseHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("live"),
});
app.UseHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("ready"),
});

app.Run();