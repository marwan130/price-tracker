namespace PriceTracker.Tests.Unit.Services;

using FluentAssertions;
using Moq;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Application.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<ICurrencyRepository> _currencyRepositoryMock;
    private readonly Mock<IStoreRepository> _storeRepositoryMock;
    private readonly Mock<IListingRepository> _listingRepositoryMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepositoryMock;
    private readonly Mock<IProductSearchService> _productSearchServiceMock;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _currencyRepositoryMock = new Mock<ICurrencyRepository>();
        _storeRepositoryMock = new Mock<IStoreRepository>();
        _listingRepositoryMock = new Mock<IListingRepository>();
        _priceHistoryRepositoryMock = new Mock<IPriceHistoryRepository>();
        _productSearchServiceMock = new Mock<IProductSearchService>();

        _productService = new ProductService(
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _currencyRepositoryMock.Object,
            _storeRepositoryMock.Object,
            _listingRepositoryMock.Object,
            _priceHistoryRepositoryMock.Object,
            _productSearchServiceMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingProduct_ReturnsMappedResponse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            ProductId = productId,
            Name = "Test Product",
            Brand = "Test Brand",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow
        };

        _productRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productService.GetByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.Name.Should().Be(product.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonexistentProduct_ThrowsNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _productRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var action = async () => await _productService.GetByIdAsync(productId);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByUrlAsync_WithExistingListing_ReturnsProductDetails()
    {
        // Arrange
        var url = "https://www.amazon.eg/dp/iphone-17-pro";
        var productId = Guid.NewGuid();
        var product = new Product
        {
            ProductId = productId,
            Name = "Test Product",
            Brand = "Test Brand",
            CreatedAt = DateTime.UtcNow
        };
        var listing = new StoreProductListing
        {
            ListingId = Guid.NewGuid(),
            ProductId = productId,
            ProductUrl = url,
            Product = product
        };
        var refreshed = new ProductSearchResult
        {
            Name = "Test Product",
            Price = 1234.56m,
            Currency = "USD",
            StoreName = "Amazon Egypt",
            ProductUrl = url
        };

        _listingRepositoryMock.Setup(x => x.GetByUrlAsync(url))
            .ReturnsAsync(listing);

        _productSearchServiceMock.Setup(x => x.SearchByUrlAsync(url, default))
            .ReturnsAsync(refreshed);

        _currencyRepositoryMock.Setup(x => x.ExistsByCodeAsync("USD"))
            .ReturnsAsync(true);

        _productRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productService.GetByUrlAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.Name.Should().Be(product.Name);
        _productSearchServiceMock.Verify(x => x.SearchByUrlAsync(url, default), Times.Once);
        _priceHistoryRepositoryMock.Verify(x => x.AddAsync(It.Is<PriceHistory>(ph =>
            ph.ListingId == listing.ListingId &&
            ph.Price == refreshed.Price &&
            ph.CurrencyCode == "USD")), Times.Once);
    }

    [Fact]
    public async Task GetByUrlAsync_WithNewListing_ScrapesAndImportsProduct()
    {
        // Arrange
        var url = "https://www.noon.com/egypt-en/iphone-17";
        _listingRepositoryMock.Setup(x => x.GetByUrlAsync(url))
            .ReturnsAsync((StoreProductListing?)null);

        var searchResult = new ProductSearchResult
        {
            Name = "Noon iPhone 17",
            Description = "Simulated noon product description",
            Price = 999.00m,
            Currency = "USD",
            StoreName = "Noon",
            ProductUrl = url,
            ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9",
            InStock = true
        };

        _productSearchServiceMock.Setup(x => x.SearchByUrlAsync(url, default))
            .ReturnsAsync(searchResult);

        _currencyRepositoryMock.Setup(x => x.ExistsByCodeAsync("USD"))
            .ReturnsAsync(true);

        var categories = new List<Category>();
        _categoryRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(categories);
        _categoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Category>()))
            .Callback<Category>(c => c.CategoryId = 10)
            .Returns(Task.CompletedTask);

        var stores = new List<Store> { new Store { StoreId = Guid.NewGuid(), Name = "Noon" } };
        _storeRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(stores);

        Product? savedProduct = null;
        _productRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(p => savedProduct = p)
            .Returns(Task.CompletedTask);

        _productRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => savedProduct);

        // Act
        var result = await _productService.GetByUrlAsync(url);

        // Assert
        result.Should().NotBeNull();
        savedProduct.Should().NotBeNull();
        savedProduct!.Name.Should().Be(searchResult.Name);
        savedProduct.Brand.Should().Be(searchResult.StoreName);
        savedProduct.CategoryId.Should().Be(10);
        _categoryRepositoryMock.Verify(x => x.AddAsync(It.Is<Category>(c => c.Name == "Mobile Phones")), Times.Once);
        savedProduct.Listings.Should().ContainSingle();
        savedProduct.Listings.First().ProductUrl.Should().Be(url);
        savedProduct.Listings.First().PriceHistories.Count.Should().Be(8); // 7 older points plus exact latest scrape
        savedProduct.Listings.First().PriceHistories.OrderByDescending(ph => ph.RecordedAt).First().Price.Should().Be(searchResult.Price);
        _productRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Once);
    }
}
