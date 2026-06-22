import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Search, Heart, ShoppingBag, Sparkles, X, ChevronDown } from "lucide-react";
import { apiClient } from "@/lib/api/apiClient";
import { useCurrency } from "@/context/CurrencyContext";

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

interface CategoryItem {
  categoryId: number;
  name: string;
}

const placeholderTexts = [
  "Search electronics, fashion, home...",
  "Find the best deal for your next buy",
  "Search by brand, product, or category",
];

const pageSize = 12;

// Local formatPrice helper removed in favor of useCurrency context

type SortOption = "price_asc" | "price_desc" | "name" | "stores";

const sortOptions: { value: SortOption; label: string }[] = [
  { value: "price_asc", label: "Price: Low to High" },
  { value: "price_desc", label: "Price: High to Low" },
  { value: "name", label: "Name: A-Z" },
  { value: "stores", label: "Most Stores" },
];

function SortDropdown({ value, onChange }: { value: SortOption; onChange: (value: SortOption) => void }) {
  const [isOpen, setIsOpen] = useState(false);
  const selectedOption = sortOptions.find(opt => opt.value === value);

  return (
    <div className="relative">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 bg-surface/50 border border-border-custom rounded-full px-4 py-2 text-sm text-text-secondary hover:border-primary hover:text-text-primary transition focus:border-primary focus:outline-none"
      >
        <span>{selectedOption?.label}</span>
        <ChevronDown className={`w-4 h-4 transition-transform ${isOpen ? "rotate-180" : ""}`} />
      </button>
      
      {isOpen && (
        <>
          <div className="fixed inset-0 z-10" onClick={() => setIsOpen(false)} />
          <div className="absolute right-0 mt-2 w-48 hp-glass-card rounded-2xl border border-border-custom overflow-hidden z-20">
            {sortOptions.map((option) => (
              <button
                key={option.value}
                onClick={() => {
                  onChange(option.value);
                  setIsOpen(false);
                }}
                className={`w-full text-left px-4 py-3 text-sm transition ${
                  option.value === value
                    ? "bg-primary/20 text-text-primary font-semibold"
                    : "text-text-secondary hover:bg-white/5 hover:text-text-primary"
                }`}
              >
                {option.label}
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

export function ProductsPage() {
  const { formatPrice } = useCurrency();
  const [query, setQuery] = useState("");
  const [debouncedQuery, setDebouncedQuery] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<number | null>(null);
  const [selectedStore, setSelectedStore] = useState<number | null>(null);
  const [priceRange, setPriceRange] = useState<{ min: number; max: number }>({ min: 0, max: 10000 });
  const [sortBy, setSortBy] = useState<"price_asc" | "price_desc" | "name" | "stores">("price_asc");
  const [products, setProducts] = useState<ProductSummary[]>([]);
  const [categories, setCategories] = useState<CategoryItem[]>([]);
  const [stores, setStores] = useState<{ storeId: number; name: string }[]>([]);
  const [loadingProducts, setLoadingProducts] = useState(false);
  const [loadingCategories, setLoadingCategories] = useState(true);
  const [loadingStores, setLoadingStores] = useState(true);
  const [hasSearched, setHasSearched] = useState(false);
  const [favorites, setFavorites] = useState<Record<string, boolean>>({});
  const [placeholderIndex, setPlaceholderIndex] = useState(0);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedQuery(query), 350);
    return () => window.clearTimeout(timer);
  }, [query]);

  useEffect(() => {
    const interval = window.setInterval(() => {
      setPlaceholderIndex((prev) => (prev + 1) % placeholderTexts.length);
    }, 2800);

    return () => window.clearInterval(interval);
  }, []);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setDebouncedQuery(query);
  };

  useEffect(() => {
    let active = true;

    setLoadingProducts(true);
    setHasSearched(true);

    if (!debouncedQuery) {
      // Fetch local catalog directly if query is empty
      apiClient.get("/v1/products", {
        params: {
          categoryId: selectedCategory ?? undefined,
          storeId: selectedStore ?? undefined,
          minPrice: priceRange.min > 0 ? priceRange.min : undefined,
          maxPrice: priceRange.max < 10000 ? priceRange.max : undefined,
          sortBy: sortBy,
          page: 0,
          size: pageSize,
        }
      })
      .then((res) => {
        if (active && res.data?.success && Array.isArray(res.data.data?.content)) {
          setProducts(res.data.data.content);
        } else {
          setProducts([]);
        }
      })
      .catch(() => {
        if (active) setProducts([]);
      })
      .finally(() => {
        if (active) setLoadingProducts(false);
      });
      return;
    }

    // Try the new internet-wide search first
    apiClient
      .get("/v1/products/search", {
        params: { query: debouncedQuery },
      })
      .then((res) => {
        if (!active) return null;
        if (res.data?.success && Array.isArray(res.data.data) && res.data.data.length > 0) {
          // Convert search results to product summaries
          const searchResults = res.data.data.map((result: any) => ({
            productId: result.productUrl || `search-${Date.now()}-${Math.random()}`,
            name: result.name,
            brand: result.storeName,
            category: null,
            primaryImage: result.imageUrl,
            lowestPrice: result.price,
            currency: result.currency,
            storeCount: 1,
          }));
          setProducts(searchResults);
          setLoadingProducts(false);
          return null; // Prevent execution of fallback
        } else {
          // Fallback to existing product search if internet search returns no results
          return apiClient.get("/v1/products", {
            params: {
              query: debouncedQuery,
              categoryId: selectedCategory ?? undefined,
              storeId: selectedStore ?? undefined,
              minPrice: priceRange.min > 0 ? priceRange.min : undefined,
              maxPrice: priceRange.max < 10000 ? priceRange.max : undefined,
              sortBy: sortBy,
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
        } else {
          setProducts([]);
        }
      })
      .catch(() => {
        if (active) setProducts([]);
      })
      .finally(() => {
        if (active && debouncedQuery) setLoadingProducts(false);
      });

    return () => {
      active = false;
    };
  }, [debouncedQuery, selectedCategory, selectedStore, priceRange.min, priceRange.max, sortBy]);

  useEffect(() => {
    let active = true;

    setLoadingCategories(true);
    apiClient
      .get("/v1/categories")
      .then((res) => {
        if (active && res.data?.success && Array.isArray(res.data.data)) {
          setCategories(res.data.data);
        }
      })
      .catch(() => {
        // Graceful fallback if categories are unavailable.
      })
      .finally(() => {
        if (active) setLoadingCategories(false);
      });

    return () => {
      active = false;
    };
  }, []);

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

  const selectedLabel = useMemo(() => {
    if (selectedCategory == null) return "All categories";
    return categories.find((cat) => cat.categoryId === selectedCategory)?.name ?? "All categories";
  }, [categories, selectedCategory]);

  const toggleFavorite = (productId: string) => {
    setFavorites((prev) => ({
      ...prev,
      [productId]: !prev[productId],
    }));
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
        <div className="inline-flex items-center gap-2 rounded-full border border-primary/15 bg-surface/50 px-3 py-1.5 text-sm text-text-secondary">
          <Sparkles className="h-4 w-4 text-accent" />
          <span>{selectedLabel}</span>
        </div>
      </div>

      <div className="mb-6 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between reveal" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
        <form onSubmit={handleSearchSubmit} className="relative flex-1">
          <Search className="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-text-muted" />
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={placeholderTexts[placeholderIndex]}
            className="hp-input"
            style={{ paddingLeft: "3rem", paddingRight: "3rem" }}
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
              <X className="h-4 w-4" />
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

      <div className="mb-6 flex flex-wrap gap-3 reveal" style={{ "--reveal-delay": "200ms" } as React.CSSProperties}>
        <div className="flex gap-2 overflow-x-auto pb-2">
          <button
            onClick={() => setSelectedCategory(null)}
            className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
              selectedCategory == null
                ? "bg-primary text-text-primary shadow-lg shadow-primary/15"
                : "bg-white/5 text-text-secondary hover:bg-white/10 hover:text-text-primary"
            }`}
          >
            All Categories
          </button>
          {loadingCategories ? (
            Array.from({ length: 5 }).map((_, index) => (
              <div
                key={index}
                className="h-9 w-24 animate-pulse rounded-full bg-white/5"
              />
            ))
          ) : (
            categories.map((category) => (
              <button
                key={category.categoryId}
                onClick={() => setSelectedCategory(category.categoryId)}
                className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
                  selectedCategory === category.categoryId
                    ? "bg-primary text-text-primary shadow-lg shadow-primary/15"
                    : "bg-white/5 text-text-secondary hover:bg-white/10 hover:text-text-primary"
                }`}
              >
                {category.name}
              </button>
            ))
          )}
        </div>
      </div>

      <div className="mb-6 flex flex-wrap gap-3 reveal" style={{ "--reveal-delay": "300ms" } as React.CSSProperties}>
        <div className="flex gap-2 overflow-x-auto pb-2">
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
        </div>
      </div>

      <div className="mb-8 flex items-center gap-4 reveal" style={{ "--reveal-delay": "400ms" } as React.CSSProperties}>
        <div className="flex-1">
          <label className="text-xs text-text-secondary mb-2 block">Price Range: ${priceRange.min} - ${priceRange.max}</label>
          <input
            type="range"
            min="0"
            max="10000"
            step="100"
            value={priceRange.max}
            onChange={(e) => setPriceRange({ ...priceRange, max: Number(e.target.value) })}
            className="w-full h-2 bg-surface/50 rounded-lg appearance-none cursor-pointer accent-primary"
          />
        </div>
        <button
          onClick={() => {
            setSelectedCategory(null);
            setSelectedStore(null);
            setPriceRange({ min: 0, max: 10000 });
            setSortBy("price_asc");
          }}
          className="text-sm text-text-secondary hover:text-text-primary transition"
        >
          Clear Filters
        </button>
      </div>

      {!hasSearched ? (
        <div className="hp-glass-card p-16 text-center relative overflow-hidden">
          {/* Floating animated elements */}
          <div className="absolute inset-0 pointer-events-none">
            <div className="absolute top-10 left-10 w-20 h-20 rounded-full bg-primary/10 animate-float" style={{ animationDelay: '0s' }} />
            <div className="absolute top-20 right-20 w-16 h-16 rounded-full bg-accent/10 animate-float" style={{ animationDelay: '1s' }} />
            <div className="absolute bottom-20 left-1/4 w-24 h-24 rounded-full bg-success/10 animate-float" style={{ animationDelay: '2s' }} />
            <div className="absolute bottom-10 right-1/3 w-12 h-12 rounded-full bg-warning/10 animate-float" style={{ animationDelay: '1.5s' }} />
          </div>
          
          <div className="relative z-10">
            <div className="w-24 h-24 mx-auto mb-6 rounded-full bg-surface/50 flex items-center justify-center animate-scale-in">
              <Search className="w-12 h-12 text-text-muted" />
            </div>
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
          {/* Floating animated elements */}
          <div className="absolute inset-0 pointer-events-none">
            <div className="absolute top-10 left-10 w-20 h-20 rounded-full bg-primary/10 animate-float" style={{ animationDelay: '0s' }} />
            <div className="absolute top-20 right-20 w-16 h-16 rounded-full bg-accent/10 animate-float" style={{ animationDelay: '1s' }} />
            <div className="absolute bottom-20 left-1/4 w-24 h-24 rounded-full bg-success/10 animate-float" style={{ animationDelay: '2s' }} />
            <div className="absolute bottom-10 right-1/3 w-12 h-12 rounded-full bg-warning/10 animate-float" style={{ animationDelay: '1.5s' }} />
          </div>
          
          <div className="relative z-10">
            <div className="w-24 h-24 mx-auto mb-6 rounded-full bg-surface/50 flex items-center justify-center animate-scale-in">
              <ShoppingBag className="w-12 h-12 text-text-muted" />
            </div>
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
                setSelectedCategory(null);
                setSelectedStore(null);
                setPriceRange({ min: 0, max: 10000 });
                setSortBy('price_asc');
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
          {products.map((product) => {
            const isFavorite = !!favorites[product.productId];
            return (
              <article
                key={product.productId}
                className="group hp-glass-card overflow-hidden rounded-3xl border border-white/5 card-hover-lift reveal"
              >
                <div className="relative aspect-[4/3] overflow-hidden">
                  <img
                    src={product.primaryImage || "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?auto=format&fit=crop&w=900&q=80"}
                    alt={product.name}
                    className="h-full w-full object-cover transition duration-500 group-hover:scale-110"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-surface via-surface/10 to-transparent" />
                  <div className="absolute left-3 top-3 rounded-full border border-success/20 bg-success/10 px-3 py-1 text-[10px] font-black uppercase tracking-[0.2em] text-success">
                    Lowest price
                  </div>
                  <button
                    onClick={() => toggleFavorite(product.productId)}
                    className="absolute right-3 top-3 rounded-full border border-white/10 bg-surface/70 p-2 text-text-primary transition hover:scale-105 hover:bg-surface"
                  >
                    <Heart
                      className={`h-4 w-4 transition ${isFavorite ? "fill-accent-secondary text-accent-secondary" : "text-text-primary"}`}
                    />
                  </button>
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

                  <div className="flex gap-2">
                    <Link
                      to={`/products/${product.productId}`}
                      className="btn-ieee flex-1 rounded-2xl bg-primary px-4 py-2.5 text-center text-sm font-semibold text-text-primary shadow-md hover:brightness-110"
                    >
                      Track product
                    </Link>
                    <Link
                      to={`/products/${product.productId}`}
                      className="inline-flex items-center justify-center rounded-2xl border border-primary/20 bg-white/5 px-4 py-2.5 text-sm font-semibold text-text-secondary transition hover:bg-white/10 hover:text-text-primary"
                    >
                      View trends
                    </Link>
                  </div>
                </div>
              </article>
            );
          })}
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
      `}</style>
    </div>
  );
}
