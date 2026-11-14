namespace Dealio.Tests.Unit.Services;

using FluentAssertions;
using Dealio.Application.Scrapers;

public sealed class ProductPageImageMinerTests
{
    private const string NoonsPage = "https://www.noon.com/egypt-en/iphone-15-clear-case/N53431923A";
    private const string AmazonsPage = "https://www.amazon.eg/-/en/Test-Product/dp/B0TEST1234";

    private static string? Mine(string head, string page) => ProductPageImageMiner.Mine(head, new Uri(page));

    // Real Amazon head shape: og:image precedes twitter:image and itemprop, and
    // both markup orderings of property/content occur across the storefronts.
    [Fact]
    public void Mine_PrefersFirstMetaImage()
    {
        var head = """
            <html><head>
            <meta name="twitter:card" content="summary_large_image">
            <meta property="og:title" content="Free Shipping">
            <meta property="og:image" content="https://m.media-amazon.com/images/I/71k38LlSqML._AC_SL1500_.jpg">
            <meta name="twitter:image" content="https://m.media-amazon.com/images/I/secondary.jpg">
            <link rel="canonical" href="https://www.amazon.eg/-/en/dp/B0TEST1234">
            </head></html>
            """;
        Mine(head, AmazonsPage).Should().Be("https://m.media-amazon.com/images/I/71k38LlSqML._AC_SL1500_.jpg");
    }

    [Fact]
    public void Mine_HandlesContentBeforePropertyAttributeOrder()
    {
        var head = """<head><meta content="https://www.noon.com/imgs/product-main.webp" property="og:image"></head>""";
        Mine(head, NoonsPage).Should().Be("https://www.noon.com/imgs/product-main.webp");
    }

    [Fact]
    public void Mine_FallsBackToItempropAndThenJsonLd()
    {
        var itemprop = """<head><img itemprop="image" src="https://www.noon.com/imgs/itemprop.jpg"></head>""";
        Mine(itemprop, NoonsPage).Should().Be("https://www.noon.com/imgs/itemprop.jpg");

        var jsonLd = """<head><script type="application/ld+json">{"@type":"Product","image":["https://www.noon.com/imgs/jsonld.jpg"]}</script></head>""";
        Mine(jsonLd, NoonsPage).Should().Be("https://www.noon.com/imgs/jsonld.jpg");
    }

    [Fact]
    public void Mine_UpgradesHttpAndDecodesHtmlEntities()
    {
        var head = """<head><meta property="og:image" content="http://www.noon.com/imgs/photo&#46;jpg"><head>""";
        Mine(head, NoonsPage).Should().Be("https://www.noon.com/imgs/photo.jpg");
    }

    [Fact]
    public void Mine_ResolvesProtocolRelativeAndRelativePaths()
    {
        Mine("""<head><meta property="og:image" content="//cdn.noon.net/imgs/phone.webp"></head>""", NoonsPage)
            .Should().Be("https://cdn.noon.net/imgs/phone.webp");

        Mine("""<head><meta property="og:image" content="/wcsstore/products/photo.jpg"></head>""", AmazonsPage)
            .Should().Be("https://www.amazon.eg/wcsstore/products/photo.jpg");
    }

    [Fact]
    public void Mine_SkipsJunkAndReturnsNextCandidateImage()
    {
        // Payment/social chrome and amazon matters most: site chrome under
        // /images/G/ is not a product photo even inside og:image.
        var head = """
            <html><head>
            <meta property="og:image" content="https://www.noon.com/static/payment-icons/visa.png">
            <meta property="og:image" content="https://www.noon.com/static/banner-homepage.jpg">
            <meta property="og:image" content="https://www.noon.com/imgs/product-real.webp">
            </head></html>
            """;
        Mine(head, NoonsPage).Should().Be("https://www.noon.com/imgs/product-real.webp");
    }

    [Fact]
    public void Mine_ReturnsNullWithoutAnyImageSource()
    {
        var head = """<head><title>Test product</title><meta name="description" content="nothing here"></head>""";
        Mine(head, AmazonsPage).Should().BeNull();
    }

    [Fact]
    public void Mine_RejectsDataAndSafeListedSchemes()
    {
        var head = """<head><meta property="og:image" content="data:image/png;base64,AAAA"></head>""";
        Mine(head, AmazonsPage).Should().BeNull();
    }
}
