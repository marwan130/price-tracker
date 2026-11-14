namespace Dealio.Application.Services;

using Dealio.Application.DTOs.Common;
using Dealio.Application.DTOs.Products;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Application.Interfaces.Services;
using Dealio.Application.Scrapers;
using Dealio.Domain.Entities;
using Dealio.Domain.Exceptions;
using Dealio.Domain.Markets;

public class ProductService : IProductService
{
    private readonly IProductRepository  _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrencyRepository _currencyRepository;
    private readonly IStoreRepository    _storeRepository;
    private readonly IListingRepository   _listingRepository;

    public ProductService(
        IProductRepository  productRepository,
        ICategoryRepository categoryRepository,
        ICurrencyRepository currencyRepository,
        IStoreRepository    storeRepository,
        IListingRepository   listingRepository)
    {
        _productRepository  = productRepository;
        _categoryRepository = categoryRepository;
        _currencyRepository = currencyRepository;
        _storeRepository    = storeRepository;
        _listingRepository   = listingRepository;
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

    public async Task<ProductResponse> ImportSearchResultAsync(ProductSearchResult searchResult)
    {
        if (!Uri.TryCreate(searchResult.ProductUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !ScraperHelpers.IsProductDetailUrl(uri.AbsoluteUri))
            throw new Dealio.Domain.Exceptions.ValidationException("A verified HTTPS product result is required.");

        var normalizedUrl = NormalizeProductUrl(uri.AbsoluteUri);
        var existingListing = await _listingRepository.GetByUrlAsync(normalizedUrl);
        if (existingListing is not null)
            return await GetByIdAsync(existingListing.ProductId);

        return await CreateFromSearchResultAsync(uri.AbsoluteUri, normalizedUrl, searchResult);
    }

    private async Task<ProductResponse> CreateFromSearchResultAsync(
        string url, string normalizedUrl, ProductSearchResult searchResult)
    {
        if (searchResult.Price <= 0
            || string.IsNullOrWhiteSpace(searchResult.Name)
            || string.IsNullOrWhiteSpace(searchResult.StoreName)
            || !IsCurrencyCode(searchResult.Currency))
        {
            throw new Dealio.Domain.Exceptions.ValidationException(
                "The page did not provide a product name, positive price, store, and explicit currency. No product was imported.");
        }
        var currencyCode = NormalizeCurrencyCode(searchResult.Currency);
        await EnsureCurrencyExistsAsync(currencyCode);

        var categoryName = ScraperHelpers.InferCategoryName(searchResult.Name, url);
        var category = (await _categoryRepository.GetAllAsync())
            .FirstOrDefault(c => string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));
        if (category == null)
        {
            category = new Category { Name = categoryName };
            await _categoryRepository.AddAsync(category);
        }
        var store = (await _storeRepository.GetAllAsync()).FirstOrDefault(s => s.Name == searchResult.StoreName);
        if (store == null)
        {
            store = new Store
            {
                StoreId = Guid.NewGuid(),
                Name = searchResult.StoreName,
                BaseUrl = new Uri(url).GetLeftPart(UriPartial.Authority),
                Country = MiddleEastMarkets.Find(searchResult.MarketCode)?.Country
                    ?? MiddleEastMarkets.All.FirstOrDefault(m => m.CurrencyCode == currencyCode)?.Country ?? "International",
                CurrencyCode = currencyCode,
                IsActive = true,
                ScraperType = Domain.Enums.ScraperType.Html,
                CreatedAt = DateTime.UtcNow
            };
            await _storeRepository.AddAsync(store);
        }

        var exactMatch = await TryAttachToExactMatchAsync(searchResult, normalizedUrl, store, currencyCode);
        if (exactMatch is not null) return exactMatch;
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Name = Limit(searchResult.Name, 300),
            // Store names do not establish product brands.
            Brand = null,
            CategoryId = category.CategoryId,
            Description = searchResult.Description,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(searchResult.ImageUrl))
        {
            // Only the image URL is stored, never image bytes: the row is a
            // pointer to the retailer CDN (object storage was dropped, and the
            // image-resolve endpoint regenerates this pointer live when the
            // stored URL rots).
            product.Images.Add(new ProductImage
            {
                ProductId = product.ProductId,
                Url = Limit(searchResult.ImageUrl, 1000),
                IsPrimary = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        var listing = new StoreProductListing
        {
            ListingId = Guid.NewGuid(),
            ProductId = product.ProductId,
            StoreId = store.StoreId,
            ProductUrl = Limit(normalizedUrl, 1000),
            IsActive = true,
            HasVerifiedPrice = searchResult.IsVerified,
            LastScrapedAt = DateTime.UtcNow
        };

        // Price history stores only values extracted from the product page.
        // Unverified discovery results mine their price from search snippets, which
        // is installment or savings text, not an observation; the tracker writes the
        // first real page observation on the next scrape.
        if (searchResult.IsVerified)
            listing.PriceHistories.Add(new PriceHistory
            {
                ListingId = listing.ListingId,
                Price = searchResult.Price,
                CurrencyCode = currencyCode,
                RecordedAt = DateTime.UtcNow,
                ScrapedAt = DateTime.UtcNow
            });

        product.Listings.Add(listing);
        await _productRepository.AddAsync(product);
        return await GetByIdAsync(product.ProductId);
    }

    private async Task<ProductResponse?> TryAttachToExactMatchAsync(
        ProductSearchResult result, string normalizedUrl, Store store, string currencyCode)
    {
        var canonicalTitle = NormalizeProductIdentity(result.Name);
        if (canonicalTitle.Length < 12) return null;

        var candidates = await _productRepository.GetAllAsync(
            new ProductFilterRequest { Query = Limit(result.Name, 300) },
            new PaginationRequest { Page = 0, Size = 50 });
        var exactIds = candidates.Content
            .Where(candidate => string.Equals(NormalizeProductIdentity(candidate.Name), canonicalTitle, StringComparison.Ordinal))
            .Select(candidate => candidate.ProductId)
            .Distinct()
            .Take(2)
            .ToArray();

        // Keep ambiguous title matches separate.
        if (exactIds.Length != 1) return null;

        var product = await _productRepository.GetByIdWithDetailsAsync(exactIds[0]);
        if (product is null) return null;
        var existingListing = await _listingRepository.GetByUrlAsync(normalizedUrl);
        if (existingListing is not null) return await GetByIdAsync(existingListing.ProductId);

        var now = DateTime.UtcNow;
        var listing = new StoreProductListing
        {
            ListingId = Guid.NewGuid(),
            ProductId = product.ProductId,
            StoreId = store.StoreId,
            ProductUrl = Limit(normalizedUrl, 1000),
            IsActive = true,
            HasVerifiedPrice = result.IsVerified,
            LastScrapedAt = now
        };
        // Same rule as the create path: snippet prices from unverified discovery
        // results are not observations and must not seed price history.
        if (result.IsVerified)
            listing.PriceHistories.Add(new PriceHistory
            {
                ListingId = listing.ListingId,
                Price = result.Price,
                CurrencyCode = currencyCode,
                RecordedAt = now,
                ScrapedAt = now
            });
        await _listingRepository.AddAsync(listing);
        return await GetByIdAsync(product.ProductId);
    }

    private static string NormalizeProductIdentity(string value)
        => string.Concat(value.Normalize(System.Text.NormalizationForm.FormKC)
            .ToLowerInvariant().Where(char.IsLetterOrDigit));

    private async Task EnsureCurrencyExistsAsync(string code)
    {
        if (await _currencyRepository.ExistsByCodeAsync(code))
            return;

        var marketCurrency = MiddleEastMarkets.All.FirstOrDefault(m => m.CurrencyCode == code);
        var (name, symbol) = marketCurrency is not null ? (marketCurrency.CurrencyName, marketCurrency.CurrencySymbol)
            : code == "USD" ? ("US Dollar", "$") : (code, code);

        await _currencyRepository.AddAsync(new Currency
        {
            Code = code,
            Name = name,
            Symbol = symbol
        });
    }

    private static string NormalizeProductUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.Trim();

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    private static string NormalizeCurrencyCode(string? currency)
    {
        return currency!.Trim().ToUpperInvariant();
    }

    private static bool IsCurrencyCode(string? currency)
        => currency is { Length: 3 } && MiddleEastMarkets.SupportedCurrencyCodes.Contains(currency);

    private static string Limit(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

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

        var (lowestPrice, currency) = GetLowestComparablePrice(latestPrices);
        var (previousPrice, previousRecordedAt, changePercent) = GetPreviousComparison(activeListings, lowestPrice, currency);

        return new ProductSummaryResponse
        {
            ProductId           = product.ProductId,
            Name                = product.Name,
            Brand               = product.Brand,
            Category            = product.Category?.Name,
            PrimaryImage        = product.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                                ?? product.Images.FirstOrDefault()?.Url,
            LowestPrice         = lowestPrice,
            Currency            = currency,
            CurrencyCode        = currency,
            StoreCount          = activeListings.Count,
            PreviousPrice       = previousPrice,
            PreviousRecordedAt  = previousRecordedAt,
            PriceChangePercent  = changePercent
        };
    }

    private static (decimal? PreviousPrice, DateTime? PreviousRecordedAt, decimal? PriceChangePercent) GetPreviousComparison(
        IReadOnlyCollection<StoreProductListing> activeListings, decimal? lowestPrice, string? currency)
    {
        if (lowestPrice is null || currency is null)
            return (null, null, null);

        // The house offer behind LowestPrice: the active, non-excluded
        // listing whose latest observation *is* that lowest price (most
        // observed one wins on ties). Previous = its immediately earlier
        // observation, with a 24h–30d window so day-to-day re-scrapes of
        // the same price don't mask the last real movement.
        var representative = activeListings
            .Where(l => !l.IsExcluded)
            .Select(l => new
            {
                Listing      = l,
                Latest       = l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).FirstOrDefault(),
                Observations = l.PriceHistories.Count
            })
            .Where(x => x.Latest != null
                        && x.Latest.Price == lowestPrice.Value
                        && string.Equals(x.Latest.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Observations)
            .Select(x => (Listing: x.Listing, Latest: x.Latest!))
            .FirstOrDefault();

        if (representative.Latest is null)
            return (null, null, null);

        var latest = representative.Latest;
        var previous = representative.Listing.PriceHistories
            .Where(ph => ph.RecordedAt <= latest.RecordedAt.AddHours(-24)
                         && ph.RecordedAt >= latest.RecordedAt.AddDays(-30))
            .OrderByDescending(ph => ph.RecordedAt)
            .FirstOrDefault();

        if (previous is null || previous.Price <= 0)
            return (null, null, null);

        var percent = Math.Round((latest.Price - previous.Price) / previous.Price * 100m, 1);
        return (previous.Price, previous.RecordedAt, percent);
    }

    private static ProductResponse MapToResponse(Product product)
    {
        var activeListings = product.Listings.Where(l => l.IsActive).ToList();
        var latestPrices = activeListings
            .Select(l => l.PriceHistories.OrderByDescending(ph => ph.RecordedAt).FirstOrDefault())
            .Where(ph => ph != null)
            .ToList();

        var (lowestPrice, currency) = GetLowestComparablePrice(latestPrices);

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
            CurrencyCode = currency
        };
    }
    private static (decimal? Price, string? Currency) GetLowestComparablePrice(IEnumerable<PriceHistory?> observations)
    {
        var values = observations.Where(p => p is not null).Select(p => p!).ToList();
        var currencies = values.Select(p => p.CurrencyCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (currencies.Count != 1) return (null, null);
        return (values.Min(p => p.Price), currencies[0]);
    }

}
