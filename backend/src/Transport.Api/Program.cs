using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Transport.Api.Contracts;
using Transport.Api.Health;
using Transport.Api.Identity;
using Transport.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .Configure<BootstrapAdminOptions>(
        builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));
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

app.UseCors("Frontend");

app.MapGet(
        "/api/system",
        () => TypedResults.Ok(
            new SystemInfoResponse(
                Name: "MapCepte Transport API",
                Runtime: ".NET 10",
                Phase: "Foundation")))
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
