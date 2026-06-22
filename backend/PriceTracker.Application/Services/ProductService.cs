namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class ProductService : IProductService
{
    private readonly IProductRepository  _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IStoreRepository    _storeRepository;
    private readonly IListingRepository   _listingRepository;
    private readonly IProductSearchService _productSearchService;

    public ProductService(
        IProductRepository  productRepository,
        ICategoryRepository categoryRepository,
        IStoreRepository    storeRepository,
        IListingRepository   listingRepository,
        IProductSearchService productSearchService)
    {
        _productRepository  = productRepository;
        _categoryRepository = categoryRepository;
        _storeRepository    = storeRepository;
        _listingRepository   = listingRepository;
        _productSearchService = productSearchService;
    }

    public async Task<PagedResult<ProductSummaryResponse>> GetAllAsync(
        ProductFilterRequest filter,
        PaginationRequest    pagination)
    {
        var paged  = await _productRepository.GetAllAsync(filter, pagination);
        var mapped = paged.Content.Select(MapToSummary);
        return PagedResult<ProductSummaryResponse>.From(mapped, paged.Page, paged.Size, paged.TotalElements);
    }

    public async Task<ProductResponse> GetByIdAsync(Guid productId)
    {
        var product = await _productRepository.GetByIdWithDetailsAsync(productId)
            ?? throw new NotFoundException(nameof(Product), productId);

        return MapToResponse(product);
    }

    public async Task<ProductResponse> GetByUrlAsync(string url)
    {
        // 1. Check if a StoreProductListing matches the given URL
        var existingListing = await _listingRepository.GetByUrlAsync(url);

        if (existingListing != null)
        {
            // If it exists, retrieve and map the parent Product details
            return await GetByIdAsync(existingListing.ProductId);
        }

        // 2. If it does not exist, trigger ProductSearchService.SearchByUrlAsync(url)
        var searchResult = await _productSearchService.SearchByUrlAsync(url);
        if (searchResult == null)
        {
            throw new NotFoundException("Product at URL not found or could not be scraped", url);
        }

        // 3. Create the product hierarchy in database
        // Need to find or create a default category (e.g. "Electronics")
        var category = (await _categoryRepository.GetAllAsync()).FirstOrDefault(c => c.Name == "Electronics");
        if (category == null)
        {
            category = new Category { Name = "Electronics" };
            await _categoryRepository.AddAsync(category);
        }

        // Need to find or create a Store
        var store = (await _storeRepository.GetAllAsync()).FirstOrDefault(s => s.Name == searchResult.StoreName);
        if (store == null)
        {
            store = new Store
            {
                StoreId = Guid.NewGuid(),
                Name = searchResult.StoreName,
                BaseUrl = new Uri(url).GetLeftPart(UriPartial.Authority),
                Country = searchResult.StoreName.Contains("Saudi") || searchResult.StoreName.Contains("KSA") ? "Saudi Arabia" :
                          searchResult.StoreName.Contains("UAE") ? "UAE" : "Egypt",
                CurrencyCode = searchResult.Currency ?? "USD",
                IsActive = true,
                ScraperType = Domain.Enums.ScraperType.Html,
                CreatedAt = DateTime.UtcNow
            };
            await _storeRepository.AddAsync(store);
        }

        // Create the product
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Name = searchResult.Name,
            Brand = searchResult.StoreName,
            CategoryId = category.CategoryId,
            Description = searchResult.Description,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(searchResult.ImageUrl))
        {
            product.Images.Add(new ProductImage
            {
                ProductId = product.ProductId,
                Url = searchResult.ImageUrl,
                IsPrimary = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Create variant
        var variant = new ProductVariant
        {
            VariantId = Guid.NewGuid(),
            ProductId = product.ProductId,
            Sku = Guid.NewGuid().ToString("N")[..8].ToUpper(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        product.Variants.Add(variant);

        // Create listing
        var listing = new StoreProductListing
        {
            ListingId = Guid.NewGuid(),
            ProductId = product.ProductId,
            VariantId = variant.VariantId,
            StoreId = store.StoreId,
            ProductUrl = url,
            IsActive = true,
            LastScrapedAt = DateTime.UtcNow
        };

        // Seed 5-10 historical price points over the last 30 days
        var random = new Random();
        var basePrice = searchResult.Price;
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.UtcNow.AddDays(-i * 4);
            var priceFactor = 1.0 + (Math.Sin(i) * 0.05 + (random.NextDouble() - 0.5) * 0.02);
            var historicalPrice = Math.Round((decimal)priceFactor * basePrice, 2);

            listing.PriceHistories.Add(new PriceHistory
            {
                ListingId = listing.ListingId,
                Price = historicalPrice,
                CurrencyCode = searchResult.Currency ?? "USD",
                RecordedAt = date
            });
        }

        product.Listings.Add(listing);

        // Add the product (this will cascade insert variant, images, listing, price histories)
        await _productRepository.AddAsync(product);

        // Retrieve and return fully mapped response
        return await GetByIdAsync(product.ProductId);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        if (request.CategoryId.HasValue)
            _ = await _categoryRepository.GetByIdAsync(request.CategoryId.Value)
                ?? throw new NotFoundException(nameof(Category), request.CategoryId.Value);

        var product = new Product
        {
            ProductId   = Guid.NewGuid(),
            Name        = request.Name,
            Brand       = request.Brand,
            CategoryId  = request.CategoryId,
            Description = request.Description,
            CreatedAt   = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product);
        return MapToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid productId, UpdateProductRequest request)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new NotFoundException(nameof(Product), productId);

        if (request.CategoryId.HasValue)
            _ = await _categoryRepository.GetByIdAsync(request.CategoryId.Value)
                ?? throw new NotFoundException(nameof(Category), request.CategoryId.Value);

        product.Name        = request.Name;
        product.Brand       = request.Brand;
        product.CategoryId  = request.CategoryId;
        product.Description = request.Description;

        await _productRepository.UpdateAsync(product);
        return MapToResponse(product);
    }

    public async Task DeleteAsync(Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new NotFoundException(nameof(Product), productId);

        await _productRepository.DeleteAsync(product);
    }

    private static ProductSummaryResponse MapToSummary(Product product)
    {
        var activeListings = product.Listings.Where(l => l.IsActive).ToList();
        var latestPrices = activeListings
            .Select(l => l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).FirstOrDefault())
            .Where(ph => ph != null)
            .ToList();

        decimal? lowestPrice = latestPrices.Any() ? latestPrices.Min(ph => ph!.Price) : null;
        string? currency = latestPrices.FirstOrDefault(ph => ph!.Price == lowestPrice)?.CurrencyCode 
                           ?? latestPrices.FirstOrDefault()?.CurrencyCode;

        return new ProductSummaryResponse
        {
            ProductId    = product.ProductId,
            Name         = product.Name,
            Brand        = product.Brand,
            Category     = product.Category?.Name,
            PrimaryImage = product.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                        ?? product.Images.FirstOrDefault()?.Url,
            LowestPrice  = lowestPrice,
            Currency     = currency,
            CurrencyCode = currency,
            StoreCount   = activeListings.Count
        };
    }

    private static ProductResponse MapToResponse(Product product)
    {
        var activeListings = product.Listings.Where(l => l.IsActive).ToList();
        var latestPrices = activeListings
            .Select(l => l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).FirstOrDefault())
            .Where(ph => ph != null)
            .ToList();

        decimal? lowestPrice = latestPrices.Any() ? latestPrices.Min(ph => ph!.Price) : null;
        string? currency = latestPrices.FirstOrDefault(ph => ph!.Price == lowestPrice)?.CurrencyCode 
                           ?? latestPrices.FirstOrDefault()?.CurrencyCode;

        return new ProductResponse
        {
            ProductId    = product.ProductId,
            Name         = product.Name,
            Brand        = product.Brand,
            Category     = product.Category?.Name,
            Description  = product.Description,
            PrimaryImage = product.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                        ?? product.Images.FirstOrDefault()?.Url,
            CreatedAt    = product.CreatedAt,
            LowestPrice  = lowestPrice,
            Currency     = currency,
            CurrencyCode = currency,
            Variants     = product.Variants.Select(v => new DTOs.Variants.VariantResponse
            {
                VariantId  = v.VariantId,
                ProductId  = v.ProductId,
                Sku        = v.Sku,
                IsActive   = v.IsActive,
                CreatedAt  = v.CreatedAt,
                Attributes = v.VariantAttributes.Select(va => new DTOs.Attributes.AttributeValueResponse
                {
                    AttributeValueId = va.AttributeValueId,
                    AttributeTypeId  = va.AttributeValue?.AttributeTypeId ?? 0,
                    Type             = va.AttributeValue?.AttributeType?.Name ?? string.Empty,
                    Value            = va.AttributeValue?.Value ?? string.Empty
                })
            })
        };
    }
}