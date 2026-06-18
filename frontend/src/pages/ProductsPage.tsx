import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Search, Heart, ShoppingBag, Sparkles, X } from "lucide-react";
import { apiClient } from "@/lib/api/apiClient";

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

function formatPrice(value: number | null | undefined, currency: string | null) {
  if (value == null) return "—";
  return `${currency ?? "USD"} ${value.toFixed(2)}`;
}

export function ProductsPage() {
  const [query, setQuery] = useState("");
  const [debouncedQuery, setDebouncedQuery] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<number | null>(null);
  const [products, setProducts] = useState<ProductSummary[]>([]);
  const [categories, setCategories] = useState<CategoryItem[]>([]);
  const [loadingProducts, setLoadingProducts] = useState(true);
  const [loadingCategories, setLoadingCategories] = useState(true);
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

  useEffect(() => {
    let active = true;

    setLoadingProducts(true);
    apiClient
      .get("/v1/products", {
        params: {
          query: debouncedQuery || undefined,
          categoryId: selectedCategory ?? undefined,
          page: 0,
          size: pageSize,
        },
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

    return () => {
      active = false;
    };
  }, [debouncedQuery, selectedCategory]);

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
      <div className="mb-8 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-xs uppercase tracking-[0.35em] text-accent font-semibold">Catalog</p>
          <h1 className="mt-2 text-3xl font-display font-black text-white md:text-4xl">
            Find your next price drop
          </h1>
        </div>
        <div className="inline-flex items-center gap-2 rounded-full border border-primary/15 bg-surface/50 px-3 py-1.5 text-sm text-text-secondary">
          <Sparkles className="h-4 w-4 text-accent" />
          <span>{selectedLabel}</span>
        </div>
      </div>

      <div className="mb-6 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="relative flex-1">
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
              onClick={() => setQuery("")}
              className="absolute right-3 top-1/2 -translate-y-1/2 rounded-full p-1 text-text-muted transition hover:bg-white/5 hover:text-white"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>
        <div className="text-sm text-text-secondary">
          {loadingProducts ? "Searching..." : `${products.length} results`}
        </div>
      </div>

      <div className="mb-8 flex gap-2 overflow-x-auto pb-2">
        <button
          onClick={() => setSelectedCategory(null)}
          className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
            selectedCategory == null
              ? "bg-primary text-white shadow-lg shadow-primary/15"
              : "bg-white/5 text-text-secondary hover:bg-white/10 hover:text-white"
          }`}
        >
          All
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
                  ? "bg-primary text-white shadow-lg shadow-primary/15"
                  : "bg-white/5 text-text-secondary hover:bg-white/10 hover:text-white"
              }`}
            >
              {category.name}
            </button>
          ))
        )}
      </div>

      {loadingProducts ? (
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
        <div className="flex min-h-[320px] flex-col items-center justify-center rounded-3xl border border-border-custom bg-surface/30 p-8 text-center text-text-secondary">
          <ShoppingBag className="mb-3 h-10 w-10 text-text-muted" />
          <p className="text-lg font-semibold text-white">No products match your filters</p>
          <p className="mt-1 max-w-md text-sm">Try a different search query or switch categories to reveal more items.</p>
        </div>
      ) : (
        <div className="grid gap-6 sm:grid-cols-2 xl:grid-cols-3">
          {products.map((product, index) => {
            const isFavorite = !!favorites[product.productId];
            return (
              <article
                key={product.productId}
                className="group hp-glass-card overflow-hidden rounded-3xl border border-white/5 card-hover-lift"
                style={{
                  animation: "catalogFadeUp 0.45s ease forwards",
                  animationDelay: `${index * 60}ms`,
                  opacity: 0,
                  transform: "translateY(18px)",
                }}
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
                    className="absolute right-3 top-3 rounded-full border border-white/10 bg-surface/70 p-2 text-white transition hover:scale-105 hover:bg-surface"
                  >
                    <Heart
                      className={`h-4 w-4 transition ${isFavorite ? "fill-accent-secondary text-accent-secondary" : "text-white"}`}
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
                    <h3 className="mt-1 line-clamp-2 text-base font-semibold text-white">
                      {product.name}
                    </h3>
                  </div>

                  <div className="flex items-end justify-between gap-3">
                    <div>
                      <p className="text-[10px] uppercase tracking-[0.28em] text-text-muted">Starting from</p>
                      <p className="mt-1 text-xl font-black text-white">
                        {formatPrice(product.lowestPrice, product.currency)}
                      </p>
                    </div>
                    <span className="rounded-full bg-white/5 px-2.5 py-1 text-xs font-semibold text-text-secondary">
                      {product.storeCount} store{product.storeCount === 1 ? "" : "s"}
                    </span>
                  </div>

                  <div className="flex gap-2">
                    <Link
                      to="/dashboard"
                      className="btn-ieee flex-1 rounded-2xl bg-primary px-4 py-2.5 text-center text-sm font-semibold text-white shadow-md hover:brightness-110"
                    >
                      Track product
                    </Link>
                    <Link
                      to="/dashboard"
                      className="inline-flex items-center justify-center rounded-2xl border border-primary/20 bg-white/5 px-4 py-2.5 text-sm font-semibold text-text-secondary transition hover:bg-white/10 hover:text-white"
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
