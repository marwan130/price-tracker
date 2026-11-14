using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Dealio.API.Extensions;
using Dealio.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.MapPlatformConfiguration();
builder.ValidateProductionSettings();

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwagger();
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHangfireServices(builder.Configuration);
builder.Services.AddValidation();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddAuthRateLimiting();
builder.Services.AddAutoMapper(_ => { }, typeof(Program).Assembly);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddExceptionHandling();
builder.Services.AddHealth();
builder.Services.AddRabbitMqWorkflow(builder.Configuration);

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    var categoryNames = new[]
    {
        "Mobile Phones", "Laptops", "Tablets", "Audio", "TVs & Monitors",
        "Wearables", "Fashion", "Home Appliances", "Furniture", "Beauty", "General"
    };
    var existingNames = (await context.Categories.Select(category => category.Name).ToListAsync())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var missingCategories = categoryNames
        .Where(name => existingNames.Add(name))
        .Select(name => new Dealio.Domain.Entities.Category { Name = name })
        .ToArray();
    if (missingCategories.Length > 0)
    {
        context.Categories.AddRange(missingCategories);
        await context.SaveChangesAsync();
        var cache = scope.ServiceProvider.GetService<Dealio.Application.Interfaces.Services.ICacheService>();
        if (cache is not null)
            await cache.RemoveAsync("categories:all");
    }
}

if (app.Environment.IsDevelopment())
    app.UseDealioSwagger();

app.UseSerilogRequestLogging();
app.UseMiddleware<Dealio.API.Infrastructure.ApiRequestMetricsMiddleware>();
app.UseDealioMiddleware();
app.UseCors();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<Dealio.API.Middleware.ActiveAccountMiddleware>();
app.UseAuthorization();
if (!app.Environment.IsEnvironment("Testing"))
    app.UseDealioHangfire(builder.Configuration);
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
