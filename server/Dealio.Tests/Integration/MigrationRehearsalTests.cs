namespace Dealio.Tests.Integration;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Dealio.Domain.Entities;
using Dealio.Infrastructure.Persistence;

public sealed class MigrationRehearsalTests : TestBase
{
    [Fact]
    public async Task LatestSearchSessionMigrationCanRollbackAndReapplyWithoutLosingSessions()
    {
        const string previousMigration = "20261103110508_AddSearchCandidateVerification";
        const string latestMigration = "20261103111449_AddTrackedProductDiscoveryTarget";
        var sessionId = Guid.NewGuid();

        using (var scope = CreateServiceScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.ProductSearchSessions.Add(new ProductSearchSession
            {
                SessionId = sessionId,
                RequestPayload = "{\"query\":\"rollback rehearsal\"}",
                Status = "completed",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
            });
            await db.SaveChangesAsync();
            await db.GetService<IMigrator>().MigrateAsync(previousMigration);
            await db.GetService<IMigrator>().MigrateAsync(latestMigration);

            var restored = await db.ProductSearchSessions.AsNoTracking().SingleAsync(item => item.SessionId == sessionId);
            restored.RequestPayload.Should().Contain("rollback rehearsal");
            restored.SourceProductId.Should().BeNull();
            restored.MarketCode.Should().BeNull();
        }
    }
}
