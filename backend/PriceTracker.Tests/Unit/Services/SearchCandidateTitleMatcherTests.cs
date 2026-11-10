namespace PriceTracker.Tests.Unit.Services;

using FluentAssertions;
using PriceTracker.Application.Scrapers;

public sealed class SearchCandidateTitleMatcherTests
{
    [Fact]
    public void Matches_StorefrontSuffixAndWordOrderChangesWhenSpecsAgree()
    {
        SearchCandidateTitleMatcher.Matches(
            "Samsung Galaxy A16 Dual SIM 4G Black 4 GB RAM 128 GB - Middle East Version",
            "Galaxy A16 4G Dual SIM Black 4GB RAM 128GB - noon UAE").Should().BeTrue();
    }

    [Theory]
    [InlineData("Samsung Galaxy A16 Dual SIM 5G Black 4GB RAM 128GB", "Galaxy A16 4G Dual SIM Black 4GB RAM 128GB - noon UAE")]
    [InlineData("Samsung Galaxy A16 Dual SIM 4G Black 6GB RAM 128GB", "Galaxy A16 4G Dual SIM Black 4GB RAM 128GB - noon UAE")]
    [InlineData("Other brand power bank 20000mAh", "Galaxy A16 4G Dual SIM Black 4GB RAM 128GB - noon UAE")]
    public void DoesNotMatch_WhenCoreModelOrSpecsConflict(string extracted, string expected)
    {
        SearchCandidateTitleMatcher.Matches(extracted, expected).Should().BeFalse();
    }

    // Real-world "airpods pro 2" result set: marketplace pages flood the query
    // with cases, covers, service plans and generics; none are the product.
    [Theory]
    [InlineData("Dado AirPods Pro 2 2nd Generation Case Cover with Keychain, Soft Silicone Skin Case Cover (Army Green)")]
    [InlineData("Easuny for AirPods Pro 2 Case Cover 2022 - Full Body Protective")]
    [InlineData("Generic Protective case for AirPods Pro 2 dark blue")]
    [InlineData("AppleCare+ for Headphones - AirPods Pro (2 Years)")]
    [InlineData("AirPods Pro Wireless Charging Case with Keychain")]
    [InlineData("Next Store Compatible with Apple Airpods Pro 2 Case Military")]
    public void IsRelevantToSearch_RejectsAccessoryTitlesNotAskedFor(string title)
    {
        SearchCandidateTitleMatcher.IsRelevantToSearch(title, "airpods pro 2").Should().BeFalse();
    }

    [Theory]
    [InlineData("Apple AirPods Pro (2nd Generation) Headphones Headset", "airpods pro 2", true)]
    [InlineData("Apple AirPods Pro (2nd Generation) with MagSafe Case (USB-C)", "airpods pro 2", true)]
    [InlineData("Apple AirPods Pro 2 Silicone Case Black", "airpods case", true)]
    [InlineData("Sonix Tech AirPods Pro 2 Earphones with Wireless Charging Case", "airpods pro 2", true)]
    [InlineData("Dado AirPods Pro 2 Case Cover Keychain Silicone Skin", "airpods pro 2", false)]
    [InlineData("OnePlus Buds Pro 2 Wireless Earbuds", "airpods pro 2", false)]
    public void IsRelevantToSearch_KeepsGenuineProductTitlesAndHonorsAccessoryQueries(string title, string query, bool expected)
    {
        SearchCandidateTitleMatcher.IsRelevantToSearch(title, query).Should().Be(expected);
    }
}
