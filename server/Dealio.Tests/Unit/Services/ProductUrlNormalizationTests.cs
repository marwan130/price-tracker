namespace Dealio.Tests.Unit.Services;

using FluentAssertions;
using Dealio.Application.Scrapers;

public sealed class ProductUrlNormalizationTests
{
    [Theory]
    [InlineData("https://www.amazon.ae/Samsung-Phone/dp/B0TEST1234?ref=search&psc=1", "amazon.ae/dp/B0TEST1234")]
    [InlineData("https://amazon.ae/dp/B0TEST1234/ref=sr_1_1?dib=tracking", "amazon.ae/dp/B0TEST1234")]
    [InlineData("https://www.noon.com/uae-en/~seller/phone/N123V/p/?o=offer1", "noon.com/uae-en/N123V/p")]
    [InlineData("https://www.noon.com/uae-en/phone/N123V/p/?o=offer2", "noon.com/uae-en/N123V/p")]
    [InlineData("https://www.amazon.ae/dp/B0TEST1234?ref=search&amp%3Bpsc=1", "amazon.ae/dp/B0TEST1234")]
    public void NormalizeProductUrl_RemovesRetailerTrackingAndKeepsProductIdentity(string url, string expected)
    {
        ScraperHelpers.NormalizeProductUrl(url).Should().Be(expected);
    }

    [Fact]
    public void IsProductDetailUrl_AcceptsHtmlEncodedQuerySeparators()
    {
        ScraperHelpers.IsProductDetailUrl("https://www.amazon.ae/dp/B0TEST1234?ref=search&amp%3Bpsc=1")
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("https://www.noon.com/egypt-en/electronics-and-mobiles/apple-iphone-15-pro/")]
    [InlineData("https://www.noon.com/egypt-en/")]
    [InlineData("https://www.noon.com/egypt-en/brands/apple/")]
    [InlineData("https://www.noon.com/egypt-en/apple-iphone-15-pro-max?f%5Bmobile_storage%5D%5B%5D=256_gb%2F")]
    [InlineData("https://www.noon.com/saudi-en/electronics-and-mobiles/mobiles-and-accessories/apple/apple-iphone-14-pro?f%5Bis_fbn%5D%5B%5D=1%2F")]
    public void IsProductDetailUrl_RejectsNoonCatalogAndSellerLandingsWithoutProductKey(string url)
    {
        ScraperHelpers.IsProductDetailUrl(url).Should().BeFalse();
    }

    [Theory]
    [InlineData("https://www.noon.com/egypt-en/electronics-and-mobiles/apple-iphone-15-pro-128gb/N12345678/p/")]
    [InlineData("https://www.noon.com/egypt-en/~seller/store-name/phone/N123V/p/?o=offer1")]
    public void IsProductDetailUrl_AcceptsNoonProductPagesKeyedWithP(string url)
    {
        ScraperHelpers.IsProductDetailUrl(url).Should().BeTrue();
    }

    [Theory]
    [InlineData("https://www.amazon.eg/-/en/apple-iphone-15-pro-max/s?k=apple+iphone+15+pro+max&page=2")]
    [InlineData("https://www.amazon.ae/iPhone-11/b?ie=UTF8&node=18133520031")]
    [InlineData("https://www.amazon.eg/-/en/top-brands")]
    public void IsProductDetailUrl_RejectsAmazonSearchAndBrowseLandings(string url)
    {
        ScraperHelpers.IsProductDetailUrl(url).Should().BeFalse();
    }

    [Theory]
    [InlineData("https://www.amazon.eg/-/en/Apple-iPhone-15-128-GB/dp/B0CHX5QVX1")]
    [InlineData("https://www.amazon.com/gp/product/B000672SR4/ref=odd-detail")]
    public void IsProductDetailUrl_AcceptsAmazonPagesKeyedByAsin(string url)
    {
        ScraperHelpers.IsProductDetailUrl(url).Should().BeTrue();
    }

    [Theory]
    [InlineData("https://www.jumia.com.eg/mlp-iphone-15-128gb")]
    [InlineData("https://www.jumia.com.eg/slp/refurbished-iphone-15-256gb")]
    [InlineData("https://www.jumia.com.eg/catalog/apple-iphone-15")]
    public void IsProductDetailUrl_RejectsJumiaMarketingAndCatalogLandings(string url)
    {
        ScraperHelpers.IsProductDetailUrl(url).Should().BeFalse();
    }

    [Theory]
    [InlineData("https://www.jumia.com.eg/samsung-galaxy-a15-128gb-6gb-ram-blue-54697189.html")]
    [InlineData("https://www.namshi.com/uae-en/buy-nike-air-max-123456/")]
    public void IsProductDetailUrl_AcceptsPagesWithoutStoreSpecificKeys(string url)
    {
        ScraperHelpers.IsProductDetailUrl(url).Should().BeTrue();
    }

    [Theory]
    [InlineData("https://2b.com.eg/en/apple-iphone-15-128gb-blue-official-warranty.html")]
    [InlineData("https://2b.com.eg/en/new-arrival/toshiba-top-freezer-refrigerator-450-liter-no-frost-inverter-black-gr-rt624we-pgn-22.html")]
    [InlineData("https://2b.com.eg/en/home-appliances/haier-air-conditioner-1-5-hp-smart-uv-inverter-12000-btu-h-cooling-heating-r32-hsu-12khrib.html")]
    public void IsProductDetailUrl_Accepts2bPagesWithLongHyphenatedSlugs(string url)
    {
        ScraperHelpers.IsProductDetailUrl(url).Should().BeTrue();
    }

    [Theory]
    [InlineData("https://2b.com.eg/en/offers/huawei-products.html")]           // offer cluster landing
    [InlineData("https://2b.com.eg/en/offers/installment-12-select.html")]    // installment offer landing
    [InlineData("https://2b.com.eg/en/offers/toshiba-deep-freezer-no-frost-238-liters-gray-gr-ru312we-dmn.html")] // "offers" is a generic deny segment
    [InlineData("https://2b.com.eg/en/computers/laptops.html")]               // category list
    [InlineData("https://2b.com.eg/en/computers/laptops/student/hp.html")]    // brand cluster
    [InlineData("https://2b.com.eg/en/mobile-and-tablet/mobiles/apple.html")] // brand cluster
    [InlineData("https://2b.com.eg/en/blog/post/toshiba-al-arabi-electrical-appliances-prices.html")] // blog
    [InlineData("https://2b.com.eg/en/2b-iso-9001-2015-certification")]       // marketing page
    [InlineData("https://2b.com.eg/en")]                                      // home
    public void IsProductDetailUrl_Rejects2bListingsAndMarketingLandings(string url)
    {
        ScraperHelpers.IsProductDetailUrl(url).Should().BeFalse();
    }

    [Theory]
    [InlineData("https://www.ubuy.com.sa/en/product/U4YWG680Q-how-to-create-your-first-digital-product")]
    [InlineData("https://www.ubuy.com.sa/en/product/N9RC2A5IW-e-commerce-product-manager-t-shirt")]
    [InlineData("https://www.ubuy.com.eg/en/product/A9BCD1234-some-product-title")]
    [InlineData("https://ubuy.ae/en/product/A9BCD1234-some-product-title")]
    public void IsProductDetailUrl_AcceptsUbuyPagesKeyedByProductId(string url)
    {
        ScraperHelpers.IsProductDetailUrl(url).Should().BeTrue();
    }

    [Theory]
    [InlineData("https://www.ubuy.com.sa/en/brand/bestbuy")]               // brand listing
    [InlineData("https://www.ubuy.com.eg/en/brand/value-product")]         // brand listing
    [InlineData("https://www.ubuy.com.sa/en/shop-import-products-from-usa")] // shopping landing
    [InlineData("https://www.ubuy.com.sa/en")]                             // home
    [InlineData("https://www.ubuy.com.eg/en/product/full-blue-ceramic-coffee-set")] // PDP missing its product ID
    public void IsProductDetailUrl_RejectsUbuyListingsAndIdlessPages(string url)
    {
        ScraperHelpers.IsProductDetailUrl(url).Should().BeFalse();
    }
}
