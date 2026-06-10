using PriceTracker.API.Extensions;
using PriceTracker.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwagger();
builder.Services.AddHangfireServices(builder.Configuration);
builder.Services.AddValidation();
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();
builder.Services.AddExceptionHandling();

var app = builder.Build();

await DatabaseSeeder.SeedAsync(app.Services, app.Configuration);

if (app.Environment.IsDevelopment())
    app.UsePriceTrackerSwagger();

app.UseSerilogRequestLogging();
app.UsePriceTrackerMiddleware();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UsePriceTrackerHangfire(builder.Configuration);
app.MapControllers();

app.Run();