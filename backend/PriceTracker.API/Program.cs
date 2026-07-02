using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using PriceTracker.API.Extensions;
using PriceTracker.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.MapPlatformConfiguration();
builder.ValidateProductionSettings();

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwagger();
builder.Services.AddHangfireServices(builder.Configuration);
builder.Services.AddValidation();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddAuthRateLimiting();
builder.Services.AddAutoMapper(_ => { }, typeof(Program).Assembly);
builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();
builder.Services.AddExceptionHandling();
builder.Services.AddHealth();

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

if (app.Environment.IsDevelopment())
    app.UsePriceTrackerSwagger();

app.UseSerilogRequestLogging();
app.UsePriceTrackerMiddleware();
app.UseCors();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UsePriceTrackerHangfire(builder.Configuration);
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

app.Run();
