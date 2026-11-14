namespace Dealio.API.Infrastructure;

using Dealio.Domain.Entities;

public static class StoreScrapeHealthPolicy
{
    public const int FailureThreshold = 5;
    public static readonly TimeSpan SuspensionDuration = TimeSpan.FromHours(6);

    public static bool RecordResult(Store store, bool succeeded, DateTime nowUtc)
    {
        if (succeeded)
        {
            store.ConsecutiveScrapeFailures = 0;
            store.AutoSuspendedUntilUtc = null;
            return false;
        }

        store.ConsecutiveScrapeFailures = Math.Min(FailureThreshold, store.ConsecutiveScrapeFailures + 1);
        if (store.ConsecutiveScrapeFailures < FailureThreshold
            || store.AutoSuspendedUntilUtc > nowUtc)
            return false;

        store.AutoSuspendedUntilUtc = nowUtc.Add(SuspensionDuration);
        return true;
    }
}
