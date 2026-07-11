import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiClient } from "@/lib/api/apiClient";
import { useCurrency } from "@/context/CurrencyContext";
import { ThemedDropdown } from "@/components/ui/ThemedDropdown";

interface ProductSummary {
  productId: string;
  name: string;
  brand: string | null;
  category: string | null;
  primaryImage: string | null;
  lowestPrice: number | null;
  currency: string | null;
  storeCount: number;
}


const placeholderTexts = [
  "Search electronics, fashion, home...",
  "Find the best deal for your next buy",
  "Search by brand, product, or category",
];

const pageSize = 12;

// Local formatPrice helper removed in favor of useCurrency context

type SortOption = "latest" | "price_asc" | "price_desc" | "name";

const sortOptions: { value: SortOption; label: string }[] = [
  { value: "latest", label: "Latest" },
  { value: "price_asc", label: "Price: Low to High" },
  { value: "price_desc", label: "Price: High to Low" },
  { value: "name", label: "Name: A-Z" },
];

const applyClientFilters = (
  items: ProductSummary[],
  minPrice: number,
  maxPrice: number | null,
  sortBy: SortOption,
  categoryName?: string,
  storeName?: string
) => {
  const filtered = items.filter((item) => {
    const price = item.lowestPrice ?? 0;
    const matchesCategory = !categoryName || inferProductCategory(item.name) === categoryName;
    const matchesStore = !storeName || item.brand === storeName;
    return price > 0
      && matchesCategory
      && matchesStore
      && (minPrice <= 0 || price >= minPrice)
      && (maxPrice == null || maxPrice <= 0 || price <= maxPrice);
  });

  return [...filtered].sort((a, b) => {
    if (sortBy === "price_asc") return (a.lowestPrice ?? Number.MAX_VALUE) - (b.lowestPrice ?? Number.MAX_VALUE);
    if (sortBy === "price_desc") return (b.lowestPrice ?? 0) - (a.lowestPrice ?? 0);
    if (sortBy === "name") return a.name.localeCompare(b.name);
    return 0;
  });
};

const inferProductCategory = (name: string) => {
  const value = name.toLowerCase();
  if (["iphone", "samsung galaxy", "smartphone", "mobile phone", "cell phone"].some(term => value.includes(term))) return "Mobile Phones";
  if (["laptop", "notebook", "macbook", "thinkpad"].some(term => value.includes(term))) return "Laptops";
  if (["tablet", "ipad"].some(term => value.includes(term))) return "Tablets";
  if (["headphone", "earbud", "airpods", "speaker"].some(term => value.includes(term))) return "Audio";
  if (["tv", "television", "monitor", "display"].some(term => value.includes(term))) return "TVs & Monitors";
  if (["watch", "smartwatch"].some(term => value.includes(term))) return "Wearables";
  if (["shoe", "shirt", "dress", "jeans", "jacket", "fashion"].some(term => value.includes(term))) return "Fashion";
  if (["fridge", "refrigerator", "washer", "microwave", "air fryer", "vacuum"].some(term => value.includes(term))) return "Home Appliances";
  if (["sofa", "chair", "table", "bed", "furniture"].some(term => value.includes(term))) return "Furniture";
  if (["makeup", "perfume", "skincare", "beauty"].some(term => value.includes(term))) return "Beauty";
  return "General";
};

function SortDropdown({ value, onChange }: { value: SortOption; onChange: (value: SortOption) => void }) {
  return (
    <ThemedDropdown
      value={value}
      options={sortOptions}
      onChange={onChange}
      className="w-48"
    />
  );
}

function ScrollButton({ direction, onClick, disabled }: { direction: 'left' | 'right'; onClick: () => void; disabled?: boolean }) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={`flex-shrink-0 w-8 h-8 rounded-full bg-white/10 hover:bg-white/20 text-text-primary flex items-center justify-center transition disabled:opacity-30 disabled:cursor-not-allowed`}
    >
      {direction === 'left' ? (
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <polyline points="15 18 9 12 15 6"></polyline>
        </svg>
      ) : (
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <polyline points="9 18 15 12 9 6"></polyline>
        </svg>
      )}
    </button>
  );
}

function ScrollableFilter({ children, scrollContainerId }: { children: React.ReactNode; scrollContainerId: string }) {
  const scroll = (direction: 'left' | 'right') => {
    const container = document.getElementById(scrollContainerId);
    if (container) {
      const scrollAmount = 200;
      container.scrollBy({ left: direction === 'left' ? -scrollAmount : scrollAmount, behavior: 'smooth' });
    }
  };

  return (
    <div className="flex items-center gap-2">
      <ScrollButton direction="left" onClick={() => scroll('left')} />
      <div id={scrollContainerId} className="flex gap-2 overflow-x-auto pb-2 scrollbar-hide" style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}>
        {children}
      </div>
      <ScrollButton direction="right" onClick={() => scroll('right')} />
    </div>
  );
}

export function ProductsPage() {
  const { formatPrice, currency } = useCurrency();
  const [query, setQuery] = useState("");
  const [debouncedQuery, setDebouncedQuery] = useState("");
  const [selectedStore, setSelectedStore] = useState<number | string | null>(null);
  const [priceInputValues, setPriceInputValues] = useState<{ min: string; max: string }>({ min: "", max: "" });
  const [sortBy, setSortBy] = useState<SortOption>("latest");
  const [products, setProducts] = useState<ProductSummary[]>([]);
  const [allSearchResults, setAllSearchResults] = useState<ProductSummary[]>([]);
  const [searchResultsQuery, setSearchResultsQuery] = useState("");
  const [searchResultsPriceKey, setSearchResultsPriceKey] = useState("");
  const [stores, setStores] = useState<{ storeId: number | string; name: string }[]>([]);
  const [loadingProducts, setLoadingProducts] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [loadingStores, setLoadingStores] = useState(true);
  const [hasSearched, setHasSearched] = useState(false);
  const [currentPage, setCurrentPage] = useState(0);
  const [hasMore, setHasMore] = useState(false);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedQuery(query), 350);
    return () => window.clearTimeout(timer);
  }, [query]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setDebouncedQuery(query);
  };

  useEffect(() => {
    let active = true;

    setLoadingProducts(true);
    setHasSearched(true);
    setCurrentPage(0);

    const minNum = Number(priceInputValues.min) || 0;
    const maxNum = priceInputValues.max === "" ? null : Number(priceInputValues.max);
    const storeName = selectedStore == null
      ? undefined
      : typeof selectedStore === "string"
        ? selectedStore
        : stores.find((store) => store.storeId === selectedStore)?.name;

    const currentPriceKey = `${minNum}-${maxNum ?? ''}`;

    // Use cached results only when BOTH the query AND price filter are unchanged
    if (debouncedQuery && searchResultsQuery === debouncedQuery && searchResultsPriceKey === currentPriceKey && allSearchResults.length > 0) {
      const filtered = applyClientFilters(allSearchResults, minNum, maxNum, sortBy, undefined, storeName);
      setProducts(filtered.slice(0, pageSize));
      setHasMore(filtered.length > pageSize);
      setLoadingProducts(false);
      return;
    }

    if (!debouncedQuery) {
      // Fetch local catalog directly if query is empty
      apiClient.get("/v1/products", {
        params: {
          storeId: typeof selectedStore === "number" ? selectedStore : undefined,
          minPrice: minNum > 0 ? minNum : undefined,
          maxPrice: maxNum != null && maxNum > 0 ? maxNum : undefined,
          sortBy: sortBy === "latest" ? undefined : sortBy,
          page: 0,
          size: pageSize,
        }
      })
      .then((res) => {
        if (active && res.data?.success && Array.isArray(res.data.data?.content)) {
          setProducts(res.data.data.content);
          setAllSearchResults((current) => current.length > 0 ? [] : current);
          setHasMore(!res.data.data.last);
        } else {
          setProducts([]);
          setHasMore(false);
        }
      })
      .catch(() => {
        if (active) {
          setProducts([]);
          setHasMore(false);
        }
      })
      .finally(() => {
        if (active) setLoadingProducts(false);
      });
      return;
    }

    // Live internet-wide search — pass price params so scraper can filter
    apiClient
      .get("/v1/products/search", {
        params: {
          query: debouncedQuery,
          minPrice: minNum > 0 ? minNum : undefined,
          maxPrice: maxNum != null && maxNum > 0 ? maxNum : undefined,
        },
      })
      .then((res) => {
        if (!active) return null;
        if (res.data?.success && Array.isArray(res.data.data) && res.data.data.length > 0) {
          const raw: ProductSummary[] = res.data.data.map((result: any) => ({
            productId: result.productUrl || `search-${Date.now()}-${Math.random()}`,
            name: result.name,
            brand: result.storeName,
            category: inferProductCategory(result.name),
            primaryImage: result.imageUrl,
            lowestPrice: result.price,
            currency: result.currency,
            storeCount: 1,
          }));
          // Save ALL raw results for client-side re-filtering (sort, category, store)
          setAllSearchResults(raw);
          setSearchResultsQuery(debouncedQuery);
          setSearchResultsPriceKey(currentPriceKey);

          // Add newly discovered stores to the filter list dynamically
          const rawStoreNames = Array.from(new Set(raw.map((item) => item.brand).filter(Boolean))) as string[];
          setStores((current) => {
            const updated = [...current];
            rawStoreNames.forEach((name) => {
              if (!updated.some((s) => s.name.toLowerCase() === name.toLowerCase())) {
                updated.push({ storeId: name, name });
              }
            });
            return updated;
          });

          const filtered = applyClientFilters(raw, minNum, maxNum, sortBy, undefined, storeName);
          setProducts(filtered.slice(0, pageSize));
          setHasMore(filtered.length > pageSize);
          setLoadingProducts(false);
          return null; // Prevent execution of fallback
        } else {
          // Fallback to local catalog
          return apiClient.get("/v1/products", {
            params: {
              query: debouncedQuery,
              storeId: typeof selectedStore === "number" ? selectedStore : undefined,
              minPrice: minNum > 0 ? minNum : undefined,
              maxPrice: maxNum != null && maxNum > 0 ? maxNum : undefined,
              sortBy: sortBy === "latest" ? undefined : sortBy,
              page: 0,
              size: pageSize,
            },
          });
        }
      })
      .then((res) => {
        if (res === null || !active) return;
        if (res.data?.success && Array.isArray(res.data.data?.content)) {
          setProducts(res.data.data.content);
          setAllSearchResults((current) => current.length > 0 ? [] : current);
          setHasMore(!res.data.data.last);
        } else {
          setProducts([]);
          setHasMore(false);
        }
      })
      .catch(() => {
        if (active) {
          setProducts([]);
          setHasMore(false);
        }
      })
      .finally(() => {
        if (active && debouncedQuery) setLoadingProducts(false);
      });

    return () => {
      active = false;
    };
  }, [debouncedQuery, selectedStore, priceInputValues.min, priceInputValues.max, sortBy, currency, stores]);


  useEffect(() => {
    let active = true;

    setLoadingStores(true);
    apiClient
      .get("/v1/stores")
      .then((res) => {
        if (active && res.data?.success && Array.isArray(res.data.data)) {
          setStores(res.data.data);
        }
      })
      .catch(() => {
        // Graceful fallback if stores are unavailable.
      })
      .finally(() => {
        if (active) setLoadingStores(false);
      });

    return () => {
      active = false;
    };
  }, []);


  const loadMoreProducts = async () => {
    if (loadingMore || !hasMore) return;

    if (allSearchResults.length > 0) {
      const minNum = Number(priceInputValues.min) || 0;
      const maxNum = priceInputValues.max === "" ? null : Number(priceInputValues.max);
      const storeName = selectedStore == null
        ? undefined
        : typeof selectedStore === "string"
          ? selectedStore
          : stores.find((store) => store.storeId === selectedStore)?.name;
      const filteredResults = applyClientFilters(allSearchResults, minNum, maxNum, sortBy, undefined, storeName);
      const nextCount = products.length + pageSize;
      setProducts(filteredResults.slice(0, nextCount));
      setHasMore(nextCount < filteredResults.length);
      return;
    }

    try {
      setLoadingMore(true);
      const nextPage = currentPage + 1;
      const minNum = Number(priceInputValues.min) || 0;
      const maxNum = priceInputValues.max === "" ? null : Number(priceInputValues.max);
      const res = await apiClient.get("/v1/products", {
        params: {
          query: debouncedQuery || undefined,
          storeId: selectedStore ?? undefined,
          minPrice: minNum > 0 ? minNum : undefined,
          maxPrice: maxNum != null && maxNum > 0 ? maxNum : undefined,
          sortBy: sortBy === "latest" ? undefined : sortBy,
          page: nextPage,
          size: pageSize,
        },
      });

      if (res.data?.success && Array.isArray(res.data.data?.content)) {
        setProducts((current) => {
          const newProducts = res.data.data.content.filter(
            (p: any) => !current.some((c) => c.productId === p.productId || c.name === p.name)
          );
          return [...current, ...newProducts];
        });
        setCurrentPage(nextPage);
        setHasMore(!res.data.data.last);
      }
    } finally {
      setLoadingMore(false);
    }
  };

  return (
    <div className="container mx-auto max-w-7xl px-4 py-8 relative z-10">
      <div className="mb-8 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between reveal">
        <div>
          <p className="text-xs uppercase tracking-[0.35em] text-accent font-semibold">Catalog</p>
          <h1 className="mt-2 text-3xl font-display font-black text-text-primary md:text-4xl">
            Find your next price drop
          </h1>
        </div>
      </div>

      <div className="mb-6 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between reveal" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
        <form onSubmit={handleSearchSubmit} className="relative flex-1">
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={placeholderTexts[0]}
            className="hp-input"
            style={{ paddingLeft: "1rem", paddingRight: "3rem" }}
          />
          {query && (
            <button
              type="button"
              onClick={() => {
                setQuery("");
                setDebouncedQuery("");
              }}
              className="absolute right-3 top-1/2 -translate-y-1/2 rounded-full p-1 text-text-muted transition hover:bg-white/5 hover:text-text-primary"
            >
              ×
            </button>
          )}
        </form>
        <div className="flex items-center gap-3">
          <SortDropdown value={sortBy} onChange={setSortBy} />
          <div className="text-sm text-text-secondary">
            {loadingProducts ? "Searching..." : `${products.length} results`}
          </div>
        </div>
      </div>


      <div className="mb-6 reveal" style={{ "--reveal-delay": "300ms" } as React.CSSProperties}>
        <ScrollableFilter scrollContainerId="store-filter">
          <button
            onClick={() => setSelectedStore(null)}
            className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
              selectedStore == null
                ? "bg-primary text-text-primary shadow-lg shadow-primary/15"
                : "bg-white/5 text-text-secondary hover:bg-white/10 hover:text-text-primary"
            }`}
          >
            All Stores
          </button>
          {loadingStores ? (
            Array.from({ length: 5 }).map((_, index) => (
              <div
                key={index}
                className="h-9 w-24 animate-pulse rounded-full bg-white/5"
              />
            ))
          ) : (
            stores.map((store) => (
              <button
                key={store.storeId}
                onClick={() => setSelectedStore(store.storeId)}
                className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
                  selectedStore === store.storeId
                    ? "bg-primary text-text-primary shadow-lg shadow-primary/15"
                    : "bg-white/5 text-text-secondary hover:bg-white/10 hover:text-text-primary"
                }`}
              >
                {store.name}
              </button>
            ))
          )}
        </ScrollableFilter>
      </div>

      <div className="mb-8 flex items-center gap-4 reveal" style={{ "--reveal-delay": "400ms" } as React.CSSProperties}>
        <div className="flex-1">
          <label className="text-xs text-text-secondary mb-2 block">Price Range ({currency})</label>
          <div className="flex gap-4 items-center">
            <div className="flex-1">
              <label className="text-[10px] text-text-muted mb-1 block">Min</label>
              <input
                type="number"
                min="0"
                max="10000"
                step="1"
                value={priceInputValues.min}
                onChange={(e) => {
                  setPriceInputValues({ min: e.target.value, max: priceInputValues.max });
                }}
                className="price-input w-full rounded-lg border border-border-custom bg-surface/50 px-3 py-2 text-sm text-text-primary outline-none focus:border-primary transition"
              />
            </div>
            <div className="flex-1">
              <label className="text-[10px] text-text-muted mb-1 block">Max</label>
              <input
                type="number"
                min="0"
                max="10000"
                step="1"
                value={priceInputValues.max}
                onChange={(e) => {
                  setPriceInputValues({ min: priceInputValues.min, max: e.target.value });
                }}
                className="price-input w-full rounded-lg border border-border-custom bg-surface/50 px-3 py-2 text-sm text-text-primary outline-none focus:border-primary transition"
              />
            </div>
          </div>
        </div>
        <button
          onClick={() => {
            setSelectedStore(null);
            setPriceInputValues({ min: "", max: "" });
            setSortBy("latest");
          }}
          className="text-sm text-text-secondary hover:text-text-primary transition"
        >
          Clear Filters
        </button>
      </div>

      {!hasSearched ? (
        <div className="hp-glass-card p-16 text-center relative overflow-hidden">
          <div className="relative z-10">
            <h3 className="text-2xl font-display font-bold text-text-primary mb-3">Search for products</h3>
            <p className="text-text-secondary mb-6 max-w-md mx-auto">
              Enter a product name above to search across multiple stores and find the best prices available online.
            </p>
          </div>
        </div>
      ) : loadingProducts ? (
        <div className="grid gap-6 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 6 }).map((_, index) => (
            <div
              key={index}
              className="hp-glass-card h-[340px] animate-pulse overflow-hidden rounded-3xl border border-white/5"
            >
              <div className="h-48 bg-white/5" />
              <div className="space-y-3 p-5">
                <div className="h-4 w-2/3 rounded bg-white/5" />
                <div className="h-4 w-1/2 rounded bg-white/5" />
                <div className="h-10 rounded bg-white/5" />
              </div>
            </div>
          ))}
        </div>
      ) : products.length === 0 ? (
        <div className="hp-glass-card p-16 text-center relative overflow-hidden">
          <div className="relative z-10">
            <h3 className="text-2xl font-display font-bold text-text-primary mb-3">No products found</h3>
            <p className="text-text-secondary mb-6 max-w-md mx-auto">
              {debouncedQuery 
                ? `No products match "${debouncedQuery}". Try a different search term or filter.`
                : "No products available in this category. Try selecting a different category."
              }
            </p>
            <button
              onClick={() => {
                setQuery('');
                setSelectedStore(null);
                setPriceInputValues({ min: "", max: "" });
                setSortBy('latest');
                setHasSearched(false);
              }}
              className="btn-ieee bg-primary px-6 py-3 rounded-full text-text-primary font-semibold hover:brightness-110 transition"
            >
              Clear Filters
            </button>
          </div>
        </div>
      ) : (
        <div className="grid gap-6 sm:grid-cols-2 xl:grid-cols-3">
          {products.map((product) => (
            <article
              key={product.productId}
              className="group hp-glass-card overflow-hidden rounded-3xl border border-white/5 reveal"
            >
                <div className="relative aspect-[4/3] overflow-hidden">
                  <img
                    src={product.primaryImage || "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?auto=format&fit=crop&w=900&q=80"}
                    alt={product.name}
                    loading="lazy"
                    decoding="async"
                    className="h-full w-full object-cover transition duration-600 ease-[cubic-bezier(0.4,0,0.2,1)] group-hover:scale-105"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-surface via-surface/10 to-transparent" />
                  <div className="absolute left-3 top-3 rounded-full border border-success/20 bg-success/10 px-3 py-1 text-[10px] font-black uppercase tracking-[0.2em] text-success">
                    Lowest price
                  </div>
                </div>

                <div className="space-y-4 p-5">
                  <div>
                    <div className="flex items-center gap-2 text-xs text-text-secondary">
                      {product.brand && <span>{product.brand}</span>}
                      {product.category && (
                        <>
                          <span>•</span>
                          <span>{product.category}</span>
                        </>
                      )}
                    </div>
                    <h3 className="mt-1 line-clamp-2 text-base font-semibold text-text-primary">
                      {product.name}
                    </h3>
                  </div>

                  <div className="flex items-end justify-between gap-3">
                    <div>
                      <p className="text-[10px] uppercase tracking-[0.28em] text-text-muted">Starting from</p>
                      <p className="mt-1 text-xl font-black text-text-primary">
                        {formatPrice(product.lowestPrice, product.currency)}
                      </p>
                    </div>
                    <span className="rounded-full bg-white/5 px-2.5 py-1 text-xs font-semibold text-text-secondary">
                      {product.storeCount} store{product.storeCount === 1 ? "" : "s"}
                    </span>
                  </div>

                  <div className="flex justify-center">
                    <Link
                      to={`/products/${encodeURIComponent(product.productId)}`}
                      className="btn-ieee w-full max-w-xs rounded-2xl bg-primary px-5 py-3 text-center text-sm font-semibold text-text-primary shadow-md hover:brightness-110"
                    >
                      Track product
                    </Link>
                  </div>
                </div>
              </article>
          ))}
          {hasMore && (
            <div className="col-span-full flex justify-center pt-2">
              <button
                onClick={loadMoreProducts}
                disabled={loadingMore}
                className="btn-ieee rounded-full border border-border-custom bg-surface px-6 py-3 text-sm font-semibold text-text-primary hover:border-primary disabled:opacity-60"
              >
                {loadingMore ? "Loading..." : "Load more products"}
              </button>
            </div>
          )}
        </div>
      )}

      <style>{`
        @keyframes catalogFadeUp {
          from {
            opacity: 0;
            transform: translateY(18px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }

        .price-input::-webkit-outer-spin-button,
        .price-input::-webkit-inner-spin-button {
          -webkit-appearance: none;
          appearance: none;
          margin: 0;
        }

        .price-input::-moz-outer-spin-button,
        .price-input::-moz-inner-spin-button {
          -moz-appearance: none;
          appearance: none;
          margin: 0;
        }

        .price-input {
          -moz-appearance: textfield;
        }
      `}</style>
    </div>
  );
}
