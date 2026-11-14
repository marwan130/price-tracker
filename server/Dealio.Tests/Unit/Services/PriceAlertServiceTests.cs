namespace Dealio.Tests.Unit.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Dealio.Application.DTOs.Currencies;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Application.Interfaces.Services;
using Dealio.Application.Services;
using Dealio.Domain.Entities;
using Dealio.Domain.Enums;

public class PriceAlertServiceTests
{
    [Fact]
    public async Task EvaluateTrackingAsync_CreatesTargetReachedEventOnce()
    {
        var fixture = new AlertFixture(85m, 100m);

        await fixture.Service.EvaluateTrackingAsync(fixture.Tracking.TrackingId);

        var notification = Assert.Single(fixture.Added);
        Assert.Equal(NotificationEventType.TargetReached, notification.EventType);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        fixture.Email.Verify(sender => sender.SendAsync("buyer@example.test", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateTrackingAsync_TargetReachedUsesTargetEmailPreference()
    {
        var fixture = new AlertFixture(85m, 100m);
        fixture.Tracking.NotifyEmail = false;

        await fixture.Service.EvaluateTrackingAsync(fixture.Tracking.TrackingId);

        Assert.Equal(NotificationStatus.Suppressed, Assert.Single(fixture.Added).Status);
        fixture.Email.Verify(sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateTrackingAsync_TargetReachedIgnoresFurtherDecreasePreference()
    {
        var fixture = new AlertFixture(85m, 100m);
        fixture.Tracking.NotifyFurtherDecreaseEmail = false;

        await fixture.Service.EvaluateTrackingAsync(fixture.Tracking.TrackingId);

        Assert.Equal(NotificationStatus.Sent, Assert.Single(fixture.Added).Status);
        fixture.Email.Verify(sender => sender.SendAsync("buyer@example.test", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateTrackingAsync_ConvertsOfferPriceToTheSavedTargetCurrencyBeforeComparing()
    {
        var fixture = new AlertFixture(60m, 100m, sourceCurrency: "USD", targetCurrency: "SAR", conversionRate: 1.5m);

        await fixture.Service.EvaluateTrackingAsync(fixture.Tracking.TrackingId);

        var notification = Assert.Single(fixture.Added);
        Assert.Equal(60m, notification.TriggeredPrice);
        Assert.Equal(90m, notification.ConvertedTriggeredPrice);
        Assert.Equal(1.5m, notification.ConversionRate);
        Assert.NotNull(notification.RateUpdatedAtUtc);
    }

    [Fact]
    public async Task EvaluateTrackingAsync_DoesNotReachTargetWhenConvertedPriceIsAboveIt()
    {
        var fixture = new AlertFixture(90m, 100m, sourceCurrency: "USD", targetCurrency: "SAR", conversionRate: 1.5m);

        await fixture.Service.EvaluateTrackingAsync(fixture.Tracking.TrackingId);

        Assert.Empty(fixture.Added);
    }

    [Fact]
    public async Task EvaluateTrackingAsync_CreatesFurtherDecreaseOnlyForANewLowerLow()
    {
        var prior = new Notification { EventType = NotificationEventType.TargetReached, TriggeredPrice = 90m };
        var fixture = new AlertFixture(85m, 100m, [prior]);

        await fixture.Service.EvaluateTrackingAsync(fixture.Tracking.TrackingId);

        Assert.Equal(NotificationEventType.FurtherDecrease, Assert.Single(fixture.Added).EventType);

        var higherObservation = new AlertFixture(92m, 100m, [prior]);
        await higherObservation.Service.EvaluateTrackingAsync(higherObservation.Tracking.TrackingId);
        Assert.Empty(higherObservation.Added);
    }

    [Fact]
    public async Task EvaluateTrackingAsync_FurtherDecreaseUsesFurtherDecreaseEmailPreference()
    {
        var prior = new Notification { EventType = NotificationEventType.TargetReached, TriggeredPrice = 90m };
        var fixture = new AlertFixture(85m, 100m, [prior]);
        fixture.Tracking.NotifyFurtherDecreaseEmail = false;

        await fixture.Service.EvaluateTrackingAsync(fixture.Tracking.TrackingId);

        Assert.Equal(NotificationStatus.Suppressed, Assert.Single(fixture.Added).Status);
        fixture.Email.Verify(sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateTrackingAsync_FurtherDecreaseIgnoresTargetReachedPreference()
    {
        var prior = new Notification { EventType = NotificationEventType.TargetReached, TriggeredPrice = 90m };
        var fixture = new AlertFixture(85m, 100m, [prior]);
        fixture.Tracking.NotifyEmail = false;

        await fixture.Service.EvaluateTrackingAsync(fixture.Tracking.TrackingId);

        Assert.Equal(NotificationStatus.Sent, Assert.Single(fixture.Added).Status);
        fixture.Email.Verify(sender => sender.SendAsync("buyer@example.test", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateTrackingAsync_DoesNotSendFurtherEmailWithinOfferCooldown()
    {
        var prior = new Notification { EventType = NotificationEventType.TargetReached, TriggeredPrice = 90m };
        var fixture = new AlertFixture(85m, 100m, [prior]);
        fixture.NotificationRepository
            .Setup(repository => repository.GetLastSentEmailAtAsync(fixture.Tracking.TrackingId, fixture.Listing.ListingId))
            .ReturnsAsync(DateTime.UtcNow.AddHours(-1));

        await fixture.Service.EvaluateTrackingAsync(fixture.Tracking.TrackingId);

        Assert.Equal(NotificationStatus.Suppressed, Assert.Single(fixture.Added).Status);
        fixture.Email.Verify(sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private sealed class AlertFixture
    {
        public readonly Mock<INotificationRepository> NotificationRepository = new();
        public readonly Mock<IEmailSender> Email = new();
        public readonly Mock<ICurrencyConversionService> CurrencyConversion = new();
        public readonly List<Notification> Added = [];
        public readonly UserProductTracking Tracking;
        public readonly StoreProductListing Listing;
        public readonly PriceAlertService Service;

        public AlertFixture(decimal price, decimal target, IReadOnlyList<Notification>? prior = null,
            string sourceCurrency = "EGP", string targetCurrency = "EGP", decimal conversionRate = 1m)
        {
            var userId = Guid.NewGuid();
            var listingId = Guid.NewGuid();
            var trackingId = Guid.NewGuid();
            var product = new Product { ProductId = Guid.NewGuid(), Name = "Example product" };
            var store = new Store { StoreId = Guid.NewGuid(), Name = "Example store" };
            Listing = new StoreProductListing { ListingId = listingId, ProductId = product.ProductId, Product = product, Store = store, IsActive = true };
            Tracking = new UserProductTracking
            {
                TrackingId = trackingId, UserId = userId, ProductId = product.ProductId,
                ListingId = listingId, TargetPrice = target, CurrencyCode = targetCurrency,
                NotifyEmail = true, NotifyFurtherDecreaseEmail = true, Product = product, Listing = Listing, IsActive = true
            };

            var trackings = new Mock<ITrackingRepository>();
            trackings.Setup(repository => repository.GetByIdAsync(trackingId)).ReturnsAsync(Tracking);
            var histories = new Mock<IPriceHistoryRepository>();
            histories.Setup(repository => repository.GetLatestByListingIdAsync(listingId))
                .ReturnsAsync(new PriceHistory { Id = 501, ListingId = listingId, Price = price, CurrencyCode = sourceCurrency, RecordedAt = DateTime.UtcNow });
            var listings = new Mock<IListingRepository>();
            listings.Setup(repository => repository.GetByIdAsync(listingId)).ReturnsAsync(Listing);
            var users = new Mock<IUserRepository>();
            users.Setup(repository => repository.GetByIdAsync(userId))
                .ReturnsAsync(new User { UserId = userId, Name = "Buyer", Email = "buyer@example.test", EmailVerified = true, IsActive = true });
            NotificationRepository.Setup(repository => repository.ExistsForPriceRecordAsync(trackingId, 501)).ReturnsAsync(false);
            NotificationRepository.Setup(repository => repository.GetForTrackingAndListingAsync(trackingId, listingId))
                .ReturnsAsync(prior ?? []);
            NotificationRepository.Setup(repository => repository.GetLastSentEmailAtAsync(trackingId, listingId)).ReturnsAsync((DateTime?)null);
            NotificationRepository.Setup(repository => repository.AddAsync(It.IsAny<Notification>()))
                .Callback<Notification>(notification => Added.Add(notification)).Returns(Task.CompletedTask);
            NotificationRepository.Setup(repository => repository.UpdateAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
            Email.Setup(sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            CurrencyConversion.Setup(service => service.ConvertAsync(It.IsAny<CurrencyConversionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrencyConversionRequest request, CancellationToken _) => new CurrencyConversionResponse
                {
                    TargetCurrency = request.TargetCurrency,
                    RateUpdatedAtUtc = DateTime.UtcNow,
                    Items = request.Items.Select(item => new CurrencyConversionResult
                    {
                        Amount = item.Amount,
                        CurrencyCode = item.CurrencyCode,
                        ConvertedAmount = Math.Round(item.Amount * conversionRate, 2),
                        ConversionRate = conversionRate
                    }).ToArray()
                });

            Service = new PriceAlertService(trackings.Object, histories.Object, NotificationRepository.Object,
                listings.Object, users.Object, Email.Object, CurrencyConversion.Object, NullLogger<PriceAlertService>.Instance);
        }
    }
}
