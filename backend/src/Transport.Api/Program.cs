using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Transport.Api.Authorization;
using Transport.Api.Calendars;
using Transport.Api.Contracts;
using Transport.Api.Health;
using Transport.Api.Identity;
using Transport.Api.RoutePaths;
using Transport.Api.Stops;
using Transport.Api.TransitLines;
using Transport.Api.Trips;
using Transport.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "MapCepte.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddPermissionAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "MapCepte.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(
        "auth-login",
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1),
            }));
});
builder.Services
    .Configure<BootstrapAdminOptions>(
        builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));
builder.Services
    .Configure<IdentitySecurityOptions>(
        builder.Configuration.GetSection(IdentitySecurityOptions.SectionName));
builder.Services.AddHostedService<AdminBootstrapHostedService>();
builder.Services
    .AddHealthChecks()
    .AddCheck<PostgisHealthCheck>(
        "postgis",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);

var frontendOrigins =
    builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "Frontend",
        policy => policy
            .WithOrigins(frontendOrigins)
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapStopEndpoints();
app.MapTransitLineEndpoints();
app.MapRoutePathEndpoints();
app.MapOperatingCalendarEndpoints();
app.MapTripEndpoints();

app.MapGet(
        "/api/system",
        () => TypedResults.Ok(
            new SystemInfoResponse(
                Name: "MapCepte Transport API",
                Runtime: ".NET 10",
                Phase: "TripAndTimetableManagement")))
    .WithName("GetSystemInfo")
    .WithTags("System");

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false,
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
    });

app.Run();

public partial class Program;
