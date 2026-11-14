namespace PriceTracker.Tests.Unit.Services;

using Moq;
using PriceTracker.Application.DTOs.Currencies;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Application.Services;
using PriceTracker.Domain.Entities;
using Xunit;

public sealed class TrackingServiceTests
{
    [Fact]
    public async Task GetByUserIdAsync_ConvertsCurrentAndTargetPriceToRequestedDisplayCurrency()
    {
        var userId = Guid.NewGuid();
        var trackingId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var tracking = new UserProductTracking
        {
            TrackingId = trackingId,
            UserId = userId,
            ProductId = Guid.NewGuid(),
            Product = new Product { Name = "Phone" },
            ListingId = listingId,
            Listing = new StoreProductListing { ListingId = listingId },
            TargetPrice = 200,
            CurrencyCode = "SAR"
        };
        var history = new PriceHistory { Id = 2, ListingId = listingId, Price = 1000, CurrencyCode = "EGP" };
        var previous = new PriceHistory { Id = 1, ListingId = listingId, Price = 2000, CurrencyCode = "EGP" };
        var repository = new Mock<ITrackingRepository>();
        repository.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync([tracking]);
        var prices = new Mock<IPriceHistoryRepository>();
        prices.Setup(x => x.GetLatestTwoByListingIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyList<PriceHistory>>
            {
                [listingId] = [history, previous]
            });
        var conversion = new Mock<ICurrencyConversionService>();
        conversion.Setup(x => x.ConvertAsync(It.IsAny<CurrencyConversionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrencyConversionRequest request, CancellationToken _) => new CurrencyConversionResponse
            {
                TargetCurrency = request.TargetCurrency,
                Items = request.Items.Select((item, index) => new CurrencyConversionResult
                {
                    Amount = item.Amount,
                    CurrencyCode = item.CurrencyCode,
                    ConvertedAmount = request.TargetCurrency switch
                    {
                        "SAR" => item.Amount / 10,
                        "AED" => item.Amount / (item.CurrencyCode == "EGP" ? 100 : 10),
                        _ => null
                    }
                }).ToList()
            });

        var service = new TrackingService(
            repository.Object,
            Mock.Of<IProductRepository>(),
            Mock.Of<IListingRepository>(),
            prices.Object,
            Mock.Of<IPriceAlertService>(),
            Mock.Of<IListingScrapeQueue>(),
            conversion.Object);

        var result = Assert.Single(await service.GetByUserIdAsync(userId, "AED"));

        Assert.Equal(100, result.CurrentPriceInTargetCurrency);
        Assert.Equal(10, result.CurrentPriceInDisplayCurrency);
        Assert.Equal(20, result.TargetPriceInDisplayCurrency);
        Assert.Equal(20, result.PreviousPriceInDisplayCurrency);
        Assert.Equal(50, result.PriceDropPercent);
        Assert.Equal(history.RecordedAt, result.CurrentPriceUpdatedAt);
        conversion.Verify(x => x.ConvertAsync(It.IsAny<CurrencyConversionRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
