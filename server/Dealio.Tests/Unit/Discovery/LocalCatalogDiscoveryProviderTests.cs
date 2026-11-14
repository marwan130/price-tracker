namespace Dealio.Tests.Unit.Discovery;

using FluentAssertions;
using Moq;
using Dealio.Application.DTOs.Products;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Domain.Entities;
using Dealio.Infrastructure.Search;

public sealed class LocalCatalogDiscoveryProviderTests
{
    [Fact]
    public async Task Search_ReturnsAnExistingVerifiedOfferWithItsOriginalCurrencyAndImage()
    {
        var offer = new StoreProductListing
        {
            ListingId = Guid.NewGuid(), ProductId = Guid.NewGuid(), StoreId = Guid.NewGuid(),
            ProductUrl = "https://www.amazon.eg/dp/B0ABC12345", IsActive = true,
            Product = new Product
            {
                Name = "Samsung Galaxy A16 128GB", Description = "Phone",
                Images = [new ProductImage { Url = "https://images.example/phone.jpg", IsPrimary = true }]
            },
            Store = new Store { Name = "Amazon Egypt", Country = "Egypt", CurrencyCode = "EGP", IsActive = true },
            PriceHistories = [new PriceHistory { Price = 8_500, CurrencyCode = "EGP", RecordedAt = DateTime.UtcNow }]
        };
        var repository = new Mock<IListingRepository>();
        repository.Setup(x => x.GetActiveListingsFilteredByPreferencesAsync("Galaxy A16", null, null, null, null, null, 0, 500))
            .ReturnsAsync([offer]);
        var provider = new LocalCatalogDiscoveryProvider(repository.Object);

        var result = await provider.SearchAsync("Galaxy A16", "EG", CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Match<ProductSearchResult>(item =>
            item.IsVerified && item.Price == 8_500 && item.Currency == "EGP" && item.MarketCode == "EG"
            && item.ProductUrl == offer.ProductUrl && item.ImageUrl == "https://images.example/phone.jpg");
    }

    [Fact]
    public async Task Search_ExcludesOffersFromAnotherMarket()
    {
        var offer = new StoreProductListing
        {
            ListingId = Guid.NewGuid(), ProductUrl = "https://www.amazon.sa/dp/B0ABC12345", IsActive = true,
            Product = new Product { Name = "Samsung Galaxy A16" },
            Store = new Store { Name = "Amazon SA", Country = "Saudi Arabia", CurrencyCode = "SAR", IsActive = true },
            PriceHistories = [new PriceHistory { Price = 700, CurrencyCode = "SAR", RecordedAt = DateTime.UtcNow }]
        };
        var repository = new Mock<IListingRepository>();
        repository.Setup(x => x.GetActiveListingsFilteredByPreferencesAsync("Galaxy A16", null, null, null, null, null, 0, 500))
            .ReturnsAsync([offer]);
        var provider = new LocalCatalogDiscoveryProvider(repository.Object);

        var result = await provider.SearchAsync("Galaxy A16", "EG", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ExcludesManuallyExcludedOffers()
    {
        var offer = new StoreProductListing
        {
            ListingId = Guid.NewGuid(), ProductUrl = "https://www.amazon.eg/dp/B0ABC12345", IsActive = true, IsExcluded = true,
            Product = new Product { Name = "Samsung Galaxy A16" },
            Store = new Store { Name = "Amazon Egypt", Country = "Egypt", CurrencyCode = "EGP", IsActive = true },
            PriceHistories = [new PriceHistory { Price = 8_500, CurrencyCode = "EGP", RecordedAt = DateTime.UtcNow }]
        };
        var repository = new Mock<IListingRepository>();
        repository.Setup(x => x.GetActiveListingsFilteredByPreferencesAsync("Galaxy A16", null, null, null, null, null, 0, 500))
            .ReturnsAsync([offer]);

        var result = await new LocalCatalogDiscoveryProvider(repository.Object)
            .SearchAsync("Galaxy A16", "EG", CancellationToken.None);

        result.Should().BeEmpty();
    }
}
