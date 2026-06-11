namespace PriceTracker.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            return;

        var email    = config["Seed:Admin:Email"];
        var password = config["Seed:Admin:Password"];
        var name     = config["Seed:Admin:Name"] ?? "System Admin";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        using var scope  = services.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher       = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger       = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        if (await db.Users.AnyAsync(u => u.Email == email))
            return;

        db.Users.Add(new User
        {
            UserId       = Guid.NewGuid(),
            Name         = name,
            Email        = email,
            PasswordHash = hasher.Hash(password),
            Role         = UserRole.Admin,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded admin user {Email}", email);
    }
}
