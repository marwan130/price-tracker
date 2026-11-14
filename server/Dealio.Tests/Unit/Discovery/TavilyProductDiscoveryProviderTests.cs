namespace Dealio.Tests.Unit.Discovery;

using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Dealio.Infrastructure.Search;

public sealed class TavilyProductDiscoveryProviderTests
{
    [Fact]
    public async Task SearchAsync_UsesTheRetailerUrlMarketInsteadOfTheRequestedSearchMarket()
    {
        const string responseJson = """
            {"results":[{"title":"Galaxy A16","url":"https://www.noon.com/qatar-en/galaxy-a16/N70126885V/p/","content":"QAR 464.85","images":["https://cdn.noon.com/galaxy-a16.webp"]}]}
            """;
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        }));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(item => item.CreateClient("product-discovery")).Returns(new HttpClient(handler));
        var provider = new TavilyProductDiscoveryProvider(factory.Object,
            Options.Create(new TavilyDiscoveryOptions { ApiKey = "test-key" }));

        var results = await provider.SearchAsync("Galaxy A16", "AE", CancellationToken.None);

        results.Should().ContainSingle();
        results[0].MarketCode.Should().Be("QA");
        results[0].Country.Should().Be("Qatar");
        results[0].Currency.Should().Be("QAR");
        results[0].ImageUrl.Should().Be("https://cdn.noon.com/galaxy-a16.webp");
    }

    [Fact]
    public async Task SearchAsync_ReadsObjectShapedSourceImagesAndRejectsUnsafeImageUrls()
    {
        const string responseJson = """
            {"results":[{"title":"Galaxy A16","url":"https://www.noon.com/uae-en/galaxy-a16/N70126885V/p/","content":"AED 594.70","images":[{"url":"http://cdn.noon.com/insecure.webp"},{"url":"https://cdn.noon.com/galaxy-a16.webp","description":"Product image"}]}]}
            """;
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        }));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(item => item.CreateClient("product-discovery")).Returns(new HttpClient(handler));
        var provider = new TavilyProductDiscoveryProvider(factory.Object,
            Options.Create(new TavilyDiscoveryOptions { ApiKey = "test-key" }));

        var results = await provider.SearchAsync("Galaxy A16", "AE", CancellationToken.None);

        results.Should().ContainSingle();
        results[0].ImageUrl.Should().Be("https://cdn.noon.com/galaxy-a16.webp");
    }

    [Fact]
    public async Task SearchAsync_SkipsSavingsAndInstallmentSnippetPricesForTheRealOne()
    {
        // The "4.71722"-scale fragments come from ratings and review counts; with
        // no contextual marker they are dropped by the amount-format rule instead.
        const string responseJson = """
            {"results":[{"title":"Noon iPhone 15 Case","url":"https://www.noon.com/egypt-en/iphone-15-case-armor-screen-protect/N12345678A/p/","content":"Official iPhone 15 protective case with clear armor glass. Or 4 interest-free payments of EGP 13.41 per month. You save EGP 48.30 compared to the original retail price. 4.71722 customers also viewed JBL Go3 Rating 4.687,222 Price: EGP 249.99","images":[]}]}
            """;
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        }));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(item => item.CreateClient("product-discovery")).Returns(new HttpClient(handler));
        var provider = new TavilyProductDiscoveryProvider(factory.Object,
            Options.Create(new TavilyDiscoveryOptions { ApiKey = "test-key" }));

        var results = await provider.SearchAsync("iphone 15 case", "EG", CancellationToken.None);

        results.Should().ContainSingle();
        results[0].Price.Should().Be(249.99m);
        results[0].Currency.Should().Be("EGP");
    }

    [Fact]
    public async Task SearchAsync_DropsStoreChromeImagesAndKeepsTheProductPhoto()
    {
        const string responseJson = """
            {"results":[{"title":"Digital Food Blender","url":"https://www.noon.com/egypt-en/blender-glass-jar-1-5l-700w/N12345678A/p/","content":"Blender 1.5L 700W. Price EGP 2,199","images":["https://f.nooncdn.com/static/version/img/banner.jpg","https://f.nooncdn.com/img/social/whatsapp.svg","https://f.nooncdn.com/p/pzsku/blender.jpg"]}]}
            """;
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        }));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(item => item.CreateClient("product-discovery")).Returns(new HttpClient(handler));
        var provider = new TavilyProductDiscoveryProvider(factory.Object,
            Options.Create(new TavilyDiscoveryOptions { ApiKey = "test-key" }));

        var results = await provider.SearchAsync("digital food blender", "EG", CancellationToken.None);

        results.Should().ContainSingle();
        results[0].ImageUrl.Should().Be("https://f.nooncdn.com/p/pzsku/blender.jpg");
    }

    [Fact]
    public async Task SearchAsync_BindsTopLevelImagesThroughTheImageSourceTitle()
    {
        // Tavily returns images as one unbound top-level array; each entry's title
        // names the crawled page. The White Joy-Con image must land on the matching
        // result only; a sale banner under Amazon's /images/G/ chrome never binds,
        // and a repeated photo URL can't decorate a second product.
        const string responseJson = """
            {
              "results": [
                {"title":"Nintendo Switch (OLED Model) - White Joy Con (UAE Version): Buy Online at Best Price in Egypt","url":"https://www.amazon.eg/-/en/Nintendo-Switch-OLED-Model-Version/dp/B098RKWHHZ","content":"AED 1,299","images":[]},
                {"title":"Nintendo Switch OLED Mario Red Console | Best Price Egypt | Cairo, Giza","url":"https://www.noon.com/egypt-en/nintendo-switch-oled-model-mario-red-edition/N70013536V/p","content":"EGP 17,500","images":[]}
              ],
              "images": [
                {"url":"https://m.media-amazon.com/images/G/42/Homepage/11.11_Sale._CB766008105_.jpg","title":"Bluetooth speakers on sale for 11.11 | Amazon.eg"},
                {"url":"https://m.media-amazon.com/images/I/61nqNujSF2L._AC_UF350,350_QL80_.jpg","title":"Nintendo Switch (OLED Model) - White Joy Con (UAE Version): Souq is now Amazon.eg"},
                {"url":"https://f.nooncdn.com/p/pzsku/other-unrelated.jpg","title":"Kitchen Blender 1.5L Best Price Egypt"},
                {"url":"https://m.media-amazon.com/images/I/61nqNujSF2L._AC_UF350,350_QL80_.jpg","title":"Nintendo Switch (OLED Model) - White Joy Con (UAE Version) renewed offer"}
              ]
            }
            """;
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        }));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(item => item.CreateClient("product-discovery")).Returns(new HttpClient(handler));
        var provider = new TavilyProductDiscoveryProvider(factory.Object,
            Options.Create(new TavilyDiscoveryOptions { ApiKey = "test-key" }));

        var results = await provider.SearchAsync("nintendo switch oled", "EG", CancellationToken.None);

        var white = results.Single(item => item.ProductUrl.Contains("B098RKWHHZ", StringComparison.Ordinal));
        var mario = results.Single(item => item.ProductUrl.Contains("N70013536V", StringComparison.Ordinal));
        white.ImageUrl.Should().Contain("61nqNujSF2L");
        mario.ImageUrl.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_PrefersTheCanonicalMainPhotoOverHarvestThumbs()
    {
        const string responseJson = """
            {"results":[{"title":"JBL Flip 6 Portable Bluetooth Speaker","url":"https://www.amazon.eg/-/en/JBL-Flip-Portable-Bluetooth-Speaker/dp/B0BL7P78BD","content":"EGP 3,050","images":["https://m.media-amazon.com/images/I/31V2R3yXC7L._AC_US40_.jpg","https://m.media-amazon.com/images/I/81-yMXi54JL._AC_SY300_SX300_QL70_ML2_.jpg","https://m.media-amazon.com/images/I/31V2R3yXC7L._AC_US40_.jpg"]}]}
            """;
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        }));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(item => item.CreateClient("product-discovery")).Returns(new HttpClient(handler));
        var provider = new TavilyProductDiscoveryProvider(factory.Object,
            Options.Create(new TavilyDiscoveryOptions { ApiKey = "test-key" }));

        var results = await provider.SearchAsync("jbl flip 6", "EG", CancellationToken.None);

        results.Should().ContainSingle();
        results[0].ImageUrl.Should().Contain("81-yMXi54JL");
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }
}
