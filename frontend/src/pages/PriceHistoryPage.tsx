import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { apiClient } from "@/lib/api/apiClient";
import { PriceHistoryChart } from "@/components/dashboard/PriceHistoryChart";
import { TrendingDown, ArrowLeft, Layers, Store, Loader2 } from "lucide-react";
import toast from "react-hot-toast";

interface Listing {
  listingId: string;
  storeId: string;
  storeName: string;
  storeUrl: string | null;
  currentPrice: number;
  currency: string;
  lastScrapedAt: string;
  isActive: boolean;
}

interface PriceDataPoint {
  priceHistoryId: string;
  price: number;
  recordedAt: string;
}

interface ProductDetail {
  productId: string;
  name: string;
  brand: string | null;
  category: string | null;
  primaryImage: string | null;
  currency: string | null;
}

type TimeRange = "7d" | "30d" | "90d" | "all";

export function PriceHistoryPage() {
  const { productId } = useParams<{ productId: string }>();
  const [product, setProduct] = useState<ProductDetail | null>(null);
  const [listings, setListings] = useState<Listing[]>([]);
  const [selectedListings, setSelectedListings] = useState<Set<string>>(new Set());
  const [priceHistoryData, setPriceHistoryData] = useState<Map<string, PriceDataPoint[]>>(new Map());
  const [loading, setLoading] = useState(true);
  const [selectedRange, setSelectedRange] = useState<TimeRange>("30d");

  useEffect(() => {
    if (!productId) return;
    
    const fetchData = async () => {
      try {
        setLoading(true);
        
        const [productRes, listingsRes] = await Promise.all([
          apiClient.get(`/v1/products/${productId}`),
          apiClient.get(`/v1/listings`, { params: { productId } })
        ]);

        if (productRes.data?.success) {
          setProduct(productRes.data.data);
        }

        if (listingsRes.data?.success && Array.isArray(listingsRes.data.data)) {
          setListings(listingsRes.data.data);
          // Select all listings by default
          setSelectedListings(new Set(listingsRes.data.data.map((l: Listing) => l.listingId)));
        }
      } catch (error) {
        toast.error("Failed to load data");
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [productId]);

  useEffect(() => {
    const fetchPriceHistory = async () => {
      if (selectedListings.size === 0) {
        setPriceHistoryData(new Map());
        return;
      }

      const historyMap = new Map<string, PriceDataPoint[]>();
      
      for (const listingId of selectedListings) {
        try {
          const res = await apiClient.get(`/v1/price-history`, {
            params: { listingId }
          });
          
          if (res.data?.success && Array.isArray(res.data.data)) {
            historyMap.set(listingId, res.data.data);
          }
        } catch (error) {
          console.error(`Failed to fetch price history for listing ${listingId}`);
        }
      }
      
      setPriceHistoryData(historyMap);
    };

    fetchPriceHistory();
  }, [selectedListings]);

  const filterPriceHistoryByRange = (history: PriceDataPoint[], range: TimeRange): PriceDataPoint[] => {
    if (range === "all") return history;
    
    const now = new Date();
    const daysMap = { "7d": 7, "30d": 30, "90d": 90 };
    const cutoffDate = new Date(now.getTime() - daysMap[range] * 24 * 60 * 60 * 1000);
    
    return history.filter(point => new Date(point.recordedAt) >= cutoffDate);
  };

  const toggleListing = (listingId: string) => {
    setSelectedListings(prev => {
      const newSet = new Set(prev);
      if (newSet.has(listingId)) {
        newSet.delete(listingId);
      } else {
        newSet.add(listingId);
      }
      return newSet;
    });
  };

  const selectAllListings = () => {
    setSelectedListings(new Set(listings.map(l => l.listingId)));
  };

  const clearAllListings = () => {
    setSelectedListings(new Set());
  };

  const getAllFilteredHistory = (): PriceDataPoint[] => {
    const allPoints: PriceDataPoint[] = [];
    for (const [, history] of priceHistoryData) {
      const filtered = filterPriceHistoryByRange(history, selectedRange);
      allPoints.push(...filtered);
    }
    // Sort by date
    return allPoints.sort((a, b) => new Date(a.recordedAt).getTime() - new Date(b.recordedAt).getTime());
  };

  if (loading) {
    return (
      <div className="container mx-auto max-w-7xl px-4 py-8">
        <div className="flex flex-1 items-center justify-center min-h-[60vh]">
          <div className="flex flex-col items-center gap-3">
            <Loader2 className="w-10 h-10 text-primary animate-spin" />
            <p className="text-text-secondary text-sm font-medium">Loading price history...</p>
          </div>
        </div>
      </div>
    );
  }

  const filteredHistory = getAllFilteredHistory();
  const defaultCurrency = product?.currency || "USD";

  return (
    <div className="container mx-auto max-w-7xl px-4 py-8 space-y-8">
      {/* Header */}
      <div className="reveal">
        <Link 
          to={`/products/${productId}`}
          className="inline-flex items-center gap-2 text-text-secondary hover:text-white transition mb-6"
        >
          <ArrowLeft className="w-4 h-4" />
          <span className="text-sm font-medium">Back to product</span>
        </Link>
        
        <h1 className="text-3xl font-display font-black tracking-tight text-white md:text-4xl">
          Price History
        </h1>
        {product && (
          <p className="text-text-secondary text-sm mt-1">
            {product.name}
          </p>
        )}
      </div>

      {/* Listing Selection */}
      <div className="reveal hp-glass-card p-6" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-display font-bold text-white flex items-center gap-2">
            <Store className="w-5 h-5 text-primary" />
            Select Stores to Compare
          </h2>
          <div className="flex gap-2">
            <button
              onClick={selectAllListings}
              className="text-xs font-semibold text-primary hover:text-primary-light transition"
            >
              Select All
            </button>
            <button
              onClick={clearAllListings}
              className="text-xs font-semibold text-text-secondary hover:text-white transition"
            >
              Clear All
            </button>
          </div>
        </div>

        <div className="flex flex-wrap gap-3">
          {listings.map((listing) => (
            <button
              key={listing.listingId}
              onClick={() => toggleListing(listing.listingId)}
              className={`px-4 py-2 rounded-full text-sm font-semibold transition ${
                selectedListings.has(listing.listingId)
                  ? "bg-primary text-white"
                  : "bg-white/10 text-text-secondary hover:bg-white/20"
              }`}
            >
              {listing.storeName}
              {selectedListings.has(listing.listingId) && (
                <span className="ml-2 text-xs opacity-75">
                  ({priceHistoryData.get(listing.listingId)?.length || 0} points)
                </span>
              )}
            </button>
          ))}
        </div>
      </div>

      {/* Time Range Selector */}
      <div className="reveal" style={{ "--reveal-delay": "200ms" } as React.CSSProperties}>
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-lg font-display font-bold text-white flex items-center gap-2">
            <TrendingDown className="w-5 h-5 text-accent" />
            Price Chart
          </h2>
          <div className="flex gap-2">
            {(["7d", "30d", "90d", "all"] as TimeRange[]).map((range) => (
              <button
                key={range}
                onClick={() => setSelectedRange(range)}
                className={`px-4 py-2 rounded-full text-sm font-semibold transition ${
                  selectedRange === range
                    ? "bg-primary text-white shadow-lg shadow-primary/15"
                    : "bg-white/5 text-text-secondary hover:bg-white/10 hover:text-white"
                }`}
              >
                {range === "all" ? "All time" : range}
              </button>
            ))}
          </div>
        </div>

        {filteredHistory.length === 0 ? (
          <div className="hp-glass-card p-8 text-center text-text-secondary">
            <TrendingDown className="w-12 h-12 mx-auto mb-4 text-text-muted" />
            <p>No price history available for the selected stores and time range.</p>
          </div>
        ) : (
          <div className="hp-glass-card p-6">
            <PriceHistoryChart 
              dataPoints={filteredHistory} 
              currencyCode={defaultCurrency} 
            />
          </div>
        )}
      </div>

      {/* Multi-dataset Toggle Layers */}
      {selectedListings.size > 1 && (
        <div className="reveal hp-glass-card p-6" style={{ "--reveal-delay": "300ms" } as React.CSSProperties}>
          <h3 className="text-lg font-display font-bold text-white mb-4 flex items-center gap-2">
            <Layers className="w-5 h-5 text-success" />
            Store Comparison Summary
          </h3>
          
          <div className="space-y-3">
            {Array.from(selectedListings).map((listingId) => {
              const listing = listings.find(l => l.listingId === listingId);
              const history = priceHistoryData.get(listingId) || [];
              const filtered = filterPriceHistoryByRange(history, selectedRange);
              
              if (!listing || filtered.length === 0) return null;
              
              const prices = filtered.map(p => p.price);
              const minPrice = Math.min(...prices);
              const maxPrice = Math.max(...prices);
              const avgPrice = prices.reduce((a, b) => a + b, 0) / prices.length;
              
              return (
                <div key={listingId} className="flex items-center justify-between p-4 bg-surface/50 rounded-xl">
                  <div className="flex items-center gap-3">
                    <div className="w-3 h-3 rounded-full bg-primary" />
                    <span className="font-medium text-white">{listing.storeName}</span>
                  </div>
                  <div className="flex gap-6 text-sm">
                    <div className="text-center">
                      <p className="text-text-secondary text-xs">Min</p>
                      <p className="font-mono font-bold text-white">{listing.currency} {minPrice.toFixed(2)}</p>
                    </div>
                    <div className="text-center">
                      <p className="text-text-secondary text-xs">Max</p>
                      <p className="font-mono font-bold text-white">{listing.currency} {maxPrice.toFixed(2)}</p>
                    </div>
                    <div className="text-center">
                      <p className="text-text-secondary text-xs">Avg</p>
                      <p className="font-mono font-bold text-text-secondary">{listing.currency} {avgPrice.toFixed(2)}</p>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
