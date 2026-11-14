namespace PriceTracker.API.Infrastructure;

using Microsoft.EntityFrameworkCore;
using PriceTracker.Infrastructure.Persistence;

public sealed class AccountDeletionWorker(IServiceScopeFactory scopeFactory, ILogger<AccountDeletionWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                await DeleteExpiredAccountsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Grace-period account deletion sweep failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DeleteExpiredAccountsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var expired = await db.Users.AsNoTracking()
            .Where(x => !x.IsActive && x.DeletionScheduledAtUtc != null && x.DeletionScheduledAtUtc <= DateTime.UtcNow)
            .Select(x => x.UserId)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var userId in expired)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.Notifications.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await db.UserProductTrackings.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await db.RefreshTokens.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            var deleted = await db.Users.Where(x => x.UserId == userId && !x.IsActive
                && x.DeletionScheduledAtUtc != null && x.DeletionScheduledAtUtc <= DateTime.UtcNow)
                .ExecuteDeleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            if (deleted > 0) logger.LogInformation("Permanently removed account {UserId} after its grace period", userId);
        }
    }
}
