namespace PriceTracker.Tests.Unit;

using PriceTracker.API.Infrastructure;
using FluentAssertions;

public sealed class PriceObservationAnomalyPolicyTests
{
    [Fact]
    public void Evaluate_AcceptsAnOrdinaryPriceChange()
    {
        var decision = PriceObservationAnomalyPolicy.Evaluate(100m, 80m, null, out var reason);

        decision.Should().Be(PriceObservationDecision.Accept);
        reason.Should().BeNull();
    }

    [Fact]
    public void Evaluate_QueuesOneRecheckForAnUnusualPriceSwing()
    {
        var decision = PriceObservationAnomalyPolicy.Evaluate(100m, 40m, null, out var reason);

        decision.Should().Be(PriceObservationDecision.Recheck);
        reason.Should().Be("unusual_price_drop");
    }

    [Fact]
    public void Evaluate_AcceptsARepeatedPriceWithinFivePercentOnTheRecheck()
    {
        var decision = PriceObservationAnomalyPolicy.Evaluate(100m, 41m, 40m, out var reason);

        decision.Should().Be(PriceObservationDecision.Accept);
        reason.Should().BeNull();
    }

    [Fact]
    public void Evaluate_RejectsARecheckThatDoesNotConfirmTheCandidate()
    {
        var decision = PriceObservationAnomalyPolicy.Evaluate(100m, 44m, 40m, out var reason);

        decision.Should().Be(PriceObservationDecision.Reject);
        reason.Should().Be("price_changed_during_anomaly_recheck");
    }

    [Fact]
    public void Evaluate_DoesNotOverflowForTheLargestDecimalObservation()
    {
        var decision = PriceObservationAnomalyPolicy.Evaluate(decimal.MaxValue, decimal.MaxValue, null, out var reason);

        decision.Should().Be(PriceObservationDecision.Accept);
        reason.Should().BeNull();
    }
}
