namespace Dealio.API.Infrastructure;

public enum PriceObservationDecision
{
    Accept,
    Recheck,
    Reject
}

public static class PriceObservationAnomalyPolicy
{
    public static PriceObservationDecision Evaluate(decimal? lastAcceptedPrice, decimal candidatePrice, decimal? expectedRecheckPrice, out string? reason)
    {
        reason = null;
        if (expectedRecheckPrice is { } expected)
        {
            var tolerance = Math.Max(0.01m, expected * 0.05m);
            if (Math.Abs(candidatePrice - expected) > tolerance)
            {
                reason = "price_changed_during_anomaly_recheck";
                return PriceObservationDecision.Reject;
            }

            return PriceObservationDecision.Accept;
        }

        // Compare halves to avoid overflowing decimal when doubling the previous price.
        if (lastAcceptedPrice is { } last && (candidatePrice < last / 2m || candidatePrice / 2m > last))
        {
            reason = candidatePrice < last / 2m ? "unusual_price_drop" : "unusual_price_increase";
            return PriceObservationDecision.Recheck;
        }

        return PriceObservationDecision.Accept;
    }
}
