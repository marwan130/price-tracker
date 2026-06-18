using PriceTracker.API.Extensions;
using PriceTracker.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();
builder.Services.AddExceptionHandling();

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

await DatabaseSeeder.SeedAsync(app.Services, app.Configuration, app.Environment);

if (app.Environment.IsDevelopment())
    app.UsePriceTrackerSwagger();

app.UseSerilogRequestLogging();
app.UsePriceTrackerMiddleware();
app.UseCors();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UsePriceTrackerHangfire(builder.Configuration);
app.MapControllers();

app.Run();