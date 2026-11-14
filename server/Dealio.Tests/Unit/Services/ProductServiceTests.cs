namespace Dealio.Tests.Unit.Services;

using FluentAssertions;
using Moq;
using Dealio.Application.DTOs.Products;
using Dealio.Application.DTOs.Common;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Application.Interfaces.Services;
using Dealio.Application.Services;
using Dealio.Domain.Entities;
using Dealio.Domain.Exceptions;
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
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _currencyRepositoryMock = new Mock<ICurrencyRepository>();
        _storeRepositoryMock = new Mock<IStoreRepository>();
        _listingRepositoryMock = new Mock<IListingRepository>();
        _productService = new ProductService(
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _currencyRepositoryMock.Object,
            _storeRepositoryMock.Object,
            _listingRepositoryMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingProduct_ReturnsMappedResponse()
    {
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
        var result = await _productService.GetByIdAsync(productId);
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.Name.Should().Be(product.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonexistentProduct_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        _productRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(productId))
            .ReturnsAsync((Product?)null);
        var action = async () => await _productService.GetByIdAsync(productId);
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ImportSearchResultAsync_WithExistingListing_ReturnsSavedProductWithoutReScraping()
    {
        var url = "https://www.amazon.eg/dp/B0CHX5QVX1";
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
        var verified = new ProductSearchResult
        {
            Name = "Test Product",
            Price = 1234.56m,
            Currency = "USD",
            StoreName = "Amazon Egypt",
            ProductUrl = url,
            IsVerified = true
        };

        _listingRepositoryMock.Setup(x => x.GetByUrlAsync(url))
            .ReturnsAsync(listing);

        _productRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(productId))
            .ReturnsAsync(product);
        var result = await _productService.ImportSearchResultAsync(verified);
        result.Should().NotBeNull();
        result.ProductId.Should().Be(productId);
        result.Name.Should().Be(product.Name);
        _listingRepositoryMock.Verify(x => x.AddAsync(It.IsAny<StoreProductListing>()), Times.Never);
    }

    [Fact]
    public async Task ImportResult_CreatesProductFromSearchCandidateWithoutMarkingThePriceVerified()
    {
        var url = "https://www.noon.com/egypt-en/iphone-17-128gb-5g-with-facetime-middle-east-version/N53433265A/p";
        _listingRepositoryMock.Setup(x => x.GetByUrlAsync(url))
            .ReturnsAsync((StoreProductListing?)null);

        var searchResult = new ProductSearchResult
        {
            Name = "Noon iPhone 17",
            Description = "Noon product description",
            Price = 999.00m,
            Currency = "USD",
            StoreName = "Noon",
            ProductUrl = url,
            ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9",
            InStock = true,
            IsVerified = false
        };

        _productRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<ProductFilterRequest>(), It.IsAny<PaginationRequest>()))
            .ReturnsAsync(PagedResult<Product>.From(Array.Empty<Product>(), 0, 50, 0));

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
        var result = await _productService.ImportSearchResultAsync(searchResult);
        result.Should().NotBeNull();
        savedProduct.Should().NotBeNull();
        savedProduct!.Name.Should().Be(searchResult.Name);
        savedProduct.Brand.Should().BeNull();
        savedProduct.CategoryId.Should().Be(10);
        _categoryRepositoryMock.Verify(x => x.AddAsync(It.Is<Category>(c => c.Name == "Mobile Phones")), Times.Once);
        savedProduct.Listings.Should().ContainSingle();
        savedProduct.Listings.First().ProductUrl.Should().Be(url);
        savedProduct.Listings.First().HasVerifiedPrice.Should().BeFalse();
        // Snippet prices are not page observations; discovery-derived amount can be
        // an installment or savings figure, so history stays empty until the tracker
        // records its first real page scrape.
        savedProduct.Listings.First().PriceHistories.Should().BeEmpty();
        _productRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task ImportResult_PersistsThePriceObservationWhenTheResultIsVerified()
    {
        var url = "https://www.noon.com/egypt-en/iphone-15-128gb-blue-5g-with-facetime-middle-east-version/N53433265A/p";
        _listingRepositoryMock.Setup(x => x.GetByUrlAsync(It.IsAny<string>()))
            .ReturnsAsync((StoreProductListing?)null);

        var searchResult = new ProductSearchResult
        {
            Name = "Noon iPhone 15 128GB Blue",
            Description = "Verified page extraction",
            Price = 30500m,
            Currency = "EGP",
            StoreName = "Noon",
            ProductUrl = url,
            ImageUrl = "https://f.nooncdn.com/p/pzsku/photos/a.jpg",
            InStock = true,
            IsVerified = true
        };

        _productRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<ProductFilterRequest>(), It.IsAny<PaginationRequest>()))
            .ReturnsAsync(PagedResult<Product>.From(Array.Empty<Product>(), 0, 50, 0));

        _currencyRepositoryMock.Setup(x => x.ExistsByCodeAsync("EGP"))
            .ReturnsAsync(true);

        _categoryRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Category>());

        _storeRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Store> { new Store { StoreId = Guid.NewGuid(), Name = "Noon" } });

        Product? savedProduct = null;
        _productRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(p => savedProduct = p)
            .Returns(Task.CompletedTask);
        _productRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => savedProduct);

        var result = await _productService.ImportSearchResultAsync(searchResult);

        result.Should().NotBeNull();
        savedProduct.Should().NotBeNull();
        savedProduct!.Listings.First().HasVerifiedPrice.Should().BeTrue();
        savedProduct.Listings.First().PriceHistories.Should().ContainSingle();
        savedProduct.Listings.First().PriceHistories.First().Price.Should().Be(searchResult.Price);
    }

    [Fact]
    public async Task ImportSearchResultAsync_AttachesOnlyAnExactNormalizedTitleToAnExistingProduct()
    {
        const string url = "https://shop.example/products/phone";
        var productId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var product = new Product
        {
            ProductId = productId,
            Name = "Apple iPhone 16 Pro 256 GB Black"
        };
        var scraped = new ProductSearchResult
        {
            Name = "Apple iPhone 16 Pro 256GB Black",
            Price = 1000m,
            Currency = "EGP",
            StoreName = "Shop",
            ProductUrl = url,
            IsVerified = true
        };

        _listingRepositoryMock.Setup(x => x.GetByUrlAsync(url)).ReturnsAsync((StoreProductListing?)null);
        _currencyRepositoryMock.Setup(x => x.ExistsByCodeAsync("EGP")).ReturnsAsync(true);
        _categoryRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Category>());
        _categoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);
        _storeRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Store> { new() { StoreId = storeId, Name = "Shop", CurrencyCode = "EGP" } });
        _productRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<ProductFilterRequest>(), It.IsAny<PaginationRequest>()))
            .ReturnsAsync(PagedResult<Product>.From(new[] { new Product { ProductId = productId, Name = "Apple iPhone 16 Pro 256 GB Black" } }, 0, 50, 1));
        _productRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(productId)).ReturnsAsync(product);
        StoreProductListing? attached = null;
        _listingRepositoryMock.Setup(x => x.AddAsync(It.IsAny<StoreProductListing>()))
            .Callback<StoreProductListing>(listing => attached = listing).Returns(Task.CompletedTask);

        var result = await _productService.ImportSearchResultAsync(scraped);

        result.ProductId.Should().Be(productId);
        attached.Should().NotBeNull();
        attached!.ProductId.Should().Be(productId);
        attached.PriceHistories.Should().ContainSingle(x => x.Price == 1000m && x.CurrencyCode == "EGP");
        _productRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Never);
    }
}
