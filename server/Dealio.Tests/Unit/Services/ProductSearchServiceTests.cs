namespace Dealio.Tests.Unit.Services;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Dealio.Application.DTOs.Common;
using Dealio.Application.DTOs.Currencies;
using Dealio.Application.DTOs.Products;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Application.Interfaces.Services;
using Dealio.Application.Services;
using Dealio.Domain.Entities;

public sealed class ProductSearchServiceTests
{
    [Fact]
    public async Task SearchProgressively_AlwaysEmitsProviderResultsAsUnverifiedCandidates()
    {
        var candidate = Candidate(isVerified: true);
        var service = CreateService(candidate);
        var emitted = new List<ProductSearchResult>();

        await service.SearchProgressivelyAsync(new ProductFilterRequest { Query = "camera", Market = "EG" },
            (item, _) => { emitted.Add(item); return Task.CompletedTask; });

        emitted.Should().ContainSingle().Which.IsVerified.Should().BeFalse();
    }

    [Fact]
    public async Task SearchProducts_ReturnsProviderCandidatesWithoutWritingObservations()
    {
        var candidate = Candidate(isVerified: true);
        var service = CreateService(candidate);

        var results = await service.SearchProductsAsync(new ProductFilterRequest { Query = "camera", Market = "EG" });

        results.Should().ContainSingle().Which.IsVerified.Should().BeFalse();
    }

    [Fact]
    public async Task SearchProgressively_DoesNotVerifyTrustedCatalogProviderResultsDuringDiscovery()
    {
        var candidate = Candidate(isVerified: true);
        var service = CreateService(candidate, verified: true);
        var emitted = new List<ProductSearchResult>();

        await service.SearchProgressivelyAsync(new ProductFilterRequest { Query = "Fixture Camera Model 42", Market = "EG" },
            (item, _) => { emitted.Add(item); return Task.CompletedTask; });

        emitted.Should().ContainSingle().Which.IsVerified.Should().BeFalse();
    }

    [Fact]
    public async Task SearchProducts_FiltersByMarketFromStoreUrlInsteadOfRequestedMarketLabel()
    {
        var candidate = Candidate(isVerified: false);
        candidate.Name = "Samsung Galaxy A16 4GB RAM 128GB";
        candidate.ProductUrl = "https://www.noon.com/qatar-en/galaxy-a16/N70126885V/p/";
        candidate.MarketCode = "AE";
        candidate.Country = "United Arab Emirates";
        candidate.Currency = "QAR";
        var service = CreateService(candidate);

        var results = await service.SearchProductsAsync(new ProductFilterRequest { Query = "Galaxy A16", Market = "AE" });

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchProgressively_RewritesCandidateMarketFromStoreUrl()
    {
        var candidate = Candidate(isVerified: false);
        candidate.Name = "Samsung Galaxy A16 4GB RAM 128GB";
        candidate.ProductUrl = "https://www.noon.com/qatar-en/galaxy-a16/N70126885V/p/";
        candidate.MarketCode = "AE";
        candidate.Country = "United Arab Emirates";
        candidate.Currency = "QAR";
        var service = CreateService(candidate);
        var emitted = new List<ProductSearchResult>();

        await service.SearchProgressivelyAsync(new ProductFilterRequest { Query = "Galaxy A16", Market = "QA" },
            (item, _) => { emitted.Add(item); return Task.CompletedTask; });

        var result = emitted.Should().ContainSingle().Which;
        result.MarketCode.Should().Be("QA");
        result.Country.Should().Be("Qatar");
    }

    [Fact]
    public async Task SearchProducts_ExcludesAccessoryResultsWhenQueryNamesTheProductSpecifications()
    {
        var candidate = Candidate(isVerified: false);
        candidate.Name = "Vansdon Case Compatible with Samsung Galaxy A16 4G/5G";
        candidate.ProductUrl = "https://www.amazon.ae/Vansdon-Compatible-Samsung-Galaxy-A16/dp/B0CQYW4VQ9";
        var service = CreateService(candidate);

        var results = await service.SearchProductsAsync(new ProductFilterRequest { Query = "Samsung Galaxy A16 4GB RAM 128GB", Market = "AE" });

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchProgressively_ExcludesCandidateThatDoesNotMatchTheRequestedProduct()
    {
        var candidate = Candidate(isVerified: false);
        candidate.Name = "Vansdon Case Compatible with Samsung Galaxy A16 4G/5G";
        var service = CreateService(candidate);
        var emitted = new List<ProductSearchResult>();

        await service.SearchProgressivelyAsync(new ProductFilterRequest { Query = "Samsung Galaxy A16 4GB RAM 128GB", Market = "AE" },
            (item, _) => { emitted.Add(item); return Task.CompletedTask; });

        emitted.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchProgressively_FiltersCandidatesBySelectedCategory()
    {
        var candidate = Candidate(isVerified: false);
        candidate.Name = "Samsung Galaxy A16 Smartphone";
        var mobileCategory = new Category { CategoryId = 12, Name = "Mobile Phones" };
        var service = CreateService(candidate, mobileCategory);
        var emitted = new List<ProductSearchResult>();

        await service.SearchProgressivelyAsync(new ProductFilterRequest { Query = "Samsung Galaxy A16", CategoryId = 12 },
            (item, _) => { emitted.Add(item); return Task.CompletedTask; });

        emitted.Should().ContainSingle().Which.Name.Should().Be(candidate.Name);
    }

    [Fact]
    public async Task SearchProgressively_ExcludesCandidatesOutsideSelectedCategory()
    {
        var candidate = Candidate(isVerified: false);
        candidate.Name = "Samsung Galaxy A16 Smartphone";
        var laptopCategory = new Category { CategoryId = 13, Name = "Laptops" };
        var service = CreateService(candidate, laptopCategory);
        var emitted = new List<ProductSearchResult>();

        await service.SearchProgressivelyAsync(new ProductFilterRequest { Query = "Samsung Galaxy A16", CategoryId = 13 },
            (item, _) => { emitted.Add(item); return Task.CompletedTask; });

        emitted.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchProgressively_KeepsCandidateWhoseConvertedPriceFitsBelowMinimum()
    {
        var candidate = Candidate(isVerified: false);
        // An AED 2,500 offer is ~EGP 34,000; a raw numeric check would drop it
        // against an EGP-10,000 minimum.
        candidate.Currency = "AED";
        candidate.Price = 2500m;
        var service = CreateService(candidate, conversion: Conversion(amount => amount * 13.6m).Object);

        var emitted = new List<ProductSearchResult>();
        await service.SearchProgressivelyAsync(new ProductFilterRequest { Query = "Fixture Camera Model 42", MinPrice = 10000 },
            (item, _) => { emitted.Add(item); return Task.CompletedTask; });

        emitted.Should().ContainSingle().Which.Price.Should().Be(2500m);
    }

    [Fact]
    public async Task SearchProgressively_DropsCandidateWhoseConvertedPriceIsBelowMinimum()
    {
        var candidate = Candidate(isVerified: false);
        candidate.Currency = "EGP";
        candidate.Price = 3000m;
        var service = CreateService(candidate, conversion: Conversion(amount => amount).Object);

        var emitted = new List<ProductSearchResult>();
        await service.SearchProgressivelyAsync(new ProductFilterRequest { Query = "Fixture Camera Model 42", MinPrice = 10000 },
            (item, _) => { emitted.Add(item); return Task.CompletedTask; });

        emitted.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchProgressively_DropsCandidateWhoseConvertedPriceExceedsMaximum()
    {
        var candidate = Candidate(isVerified: false);
        candidate.Currency = "AED";
        candidate.Price = 5000m;
        var service = CreateService(candidate, conversion: Conversion(amount => amount * 13.6m).Object);

        var emitted = new List<ProductSearchResult>();
        await service.SearchProgressivelyAsync(new ProductFilterRequest { Query = "Fixture Camera Model 42", MaxPrice = 10000 },
            (item, _) => { emitted.Add(item); return Task.CompletedTask; });

        emitted.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchProgressively_KeepsCandidatesWhenRatesAreUnavailable()
    {
        var candidate = Candidate(isVerified: false);
        candidate.Currency = "AED";
        candidate.Price = 2500m;
        var conversion = new Mock<ICurrencyConversionService>();
        conversion.Setup(s => s.ConvertAsync(It.IsAny<CurrencyConversionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("rates down"));
        var service = CreateService(candidate, conversion: conversion.Object);

        var emitted = new List<ProductSearchResult>();
        await service.SearchProgressivelyAsync(new ProductFilterRequest { Query = "Fixture Camera Model 42", MinPrice = 10000 },
            (item, _) => { emitted.Add(item); return Task.CompletedTask; });

        emitted.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchProducts_KeepsCandidateWhoseConvertedPriceFitsMinimum()
    {
        var candidate = Candidate(isVerified: false);
        candidate.Currency = "AED";
        candidate.Price = 2500m;
        var service = CreateService(candidate, conversion: Conversion(amount => amount * 13.6m).Object);

        var results = await service.SearchProductsAsync(new ProductFilterRequest { Query = "Fixture Camera Model 42", MinPrice = 10000 });

        results.Should().ContainSingle().Which.Price.Should().Be(2500m);
    }

    private static Mock<ICurrencyConversionService> Conversion(Func<decimal, decimal?> convert)
    {
        var service = new Mock<ICurrencyConversionService>();
        service.Setup(s => s.ConvertAsync(It.IsAny<CurrencyConversionRequest>(), It.IsAny<CancellationToken>()))
            .Returns((CurrencyConversionRequest request, CancellationToken _) => Task.FromResult(new CurrencyConversionResponse
            {
                TargetCurrency = request.TargetCurrency,
                Items = request.Items.Select(item => new CurrencyConversionResult
                {
                    Amount = item.Amount,
                    CurrencyCode = item.CurrencyCode,
                    ConvertedAmount = convert(item.Amount)
                }).ToList()
            }));
        return service;
    }

    private static ProductSearchService CreateService(ProductSearchResult candidate, Category? category = null, bool verified = false, ICurrencyConversionService? conversion = null)
    {
        var categories = new Mock<ICategoryRepository>();
        if (category is not null)
            categories.Setup(repository => repository.GetByIdAsync(category.CategoryId)).ReturnsAsync(category);
        var stores = new Mock<IStoreRepository>();
        var listings = new Mock<IListingRepository>();
        listings.Setup(repository => repository.GetAllAsync()).ReturnsAsync(Array.Empty<StoreProductListing>());
        return new ProductSearchService(
            NullLogger<ProductSearchService>.Instance,
            categories.Object,
            stores.Object,
            listings.Object,
            currencyConversion: conversion,
            discoveryProviders: [verified ? new VerifiedStubProvider(candidate) : new StubProvider(candidate)]);
    }

    private static ProductSearchResult Candidate(bool isVerified) => new()
    {
        Name = "Fixture Camera Model 42",
        ProductUrl = "https://shop.example/products/camera-model-42",
        StoreName = "Fixture Retailer",
        MarketCode = "EG",
        Country = "Egypt",
        Currency = "EGP",
        Price = 12500m,
        IsVerified = isVerified
    };

    private sealed class StubProvider(ProductSearchResult candidate) : IProductDiscoveryProvider
    {
        public string Name => "fixture";
        public Task<IReadOnlyList<ProductSearchResult>> SearchAsync(string query, string? market,
            CancellationToken cancellationToken, decimal? minPrice = null, decimal? maxPrice = null, int page = 0, string? categoryName = null)
            => Task.FromResult<IReadOnlyList<ProductSearchResult>>([candidate]);
        public bool HasMore(string? market, int page) => false;
    }

    private sealed class VerifiedStubProvider(ProductSearchResult candidate) : IVerifiedProductDiscoveryProvider
    {
        public string Name => "LocalCatalog";
        public Task<IReadOnlyList<ProductSearchResult>> SearchAsync(string query, string? market, CancellationToken cancellationToken,
            decimal? minPrice = null, decimal? maxPrice = null, int page = 0, string? categoryName = null) => Task.FromResult<IReadOnlyList<ProductSearchResult>>([candidate]);
        public bool HasMore(string? market, int page) => false;
    }
}
