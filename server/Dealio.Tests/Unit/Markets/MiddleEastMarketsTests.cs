namespace Dealio.Tests.Unit.Markets;

using FluentAssertions;
using Dealio.Domain.Markets;

public sealed class MiddleEastMarketsTests
{
    [Theory]
    [InlineData("https://www.noon.com/uae-en/item/N1/p/", "AE")]
    [InlineData("https://www.noon.com/egypt-ar/item/N1/p/", "EG")]
    [InlineData("https://supermall.noon.com/saudi-en/item/N1/p/", "SA")]
    [InlineData("https://www.jumia.com.eg/item-123.html", "EG")]
    [InlineData("https://www.amazon.sa/dp/123", "SA")]
    [InlineData("https://en.namshi.com/kw/item.html", "KW")]
    public void FindByProductUrl_ResolvesSupportedRetailerMarket(string value, string expectedCode)
    {
        MiddleEastMarkets.FindByProductUrl(new Uri(value))
            .Should().Be(MiddleEastMarkets.Find(expectedCode));
    }

    [Theory]
    [InlineData("https://www.amazon.com/dp/123")]
    [InlineData("https://shop.example/products/123")]
    public void FindByProductUrl_DoesNotGuessUnknownMarket(string value)
    {
        MiddleEastMarkets.FindByProductUrl(new Uri(value)).Should().BeNull();
    }
}
