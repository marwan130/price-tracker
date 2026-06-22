import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { ArrowLeft, Store, TrendingDown, Bell, Check, Sparkles, ExternalLink } from "lucide-react";
import { apiClient } from "@/lib/api/apiClient";
import toast from "react-hot-toast";
import { PriceHistoryChart } from "@/components/dashboard/PriceHistoryChart";
import { useCurrency } from "@/context/CurrencyContext";

interface ProductDetail {
  productId: string;
  name: string;
  brand: string | null;
  category: string | null;
  description: string | null;
  primaryImage: string | null;
  images: string[];
  lowestPrice: number | null;
  currency: string | null;
}

interface StoreListing {
  listingId: string;
  storeId: string;
  storeName: string;
  storeUrl: string | null;
  currentPrice: number;
  currency: string;
  lastScrapedAt: string;
  isActive: boolean;
}

interface PriceHistoryPoint {
  priceHistoryId: string;
  price: number;
  recordedAt: string;
}

type TimeRange = "7d" | "30d" | "90d" | "all";

export function ProductDetailPage() {
  const { productId } = useParams<{ productId: string }>();
  const { formatPrice, convertPrice, currency: activeCurrency } = useCurrency();
  const [product, setProduct] = useState<ProductDetail | null>(null);
  const [listings, setListings] = useState<StoreListing[]>([]);
  const [priceHistory, setPriceHistory] = useState<PriceHistoryPoint[]>([]);
  const [selectedRange, setSelectedRange] = useState<TimeRange>("30d");
  const [loading, setLoading] = useState(true);
  const [targetPrice, setTargetPrice] = useState<number>(0);
  const [notifyEmail, setNotifyEmail] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [showSuccessModal, setShowSuccessModal] = useState(false);

  useEffect(() => {
    if (!productId) return;
    
    const fetchProductData = async () => {
      try {
        setLoading(true);
        
        const [productRes, listingsRes] = await Promise.all([
          apiClient.get(`/v1/products/${productId}`),
          apiClient.get(`/v1/listings`, { params: { productId } })
        ]);

        if (productRes.data?.success) {
          setProduct(productRes.data.data);
          if (productRes.data.data.lowestPrice) {
            setTargetPrice(productRes.data.data.lowestPrice * 0.9);
          }
        }

        let fetchedListings: StoreListing[] = [];
        if (listingsRes.data?.success && Array.isArray(listingsRes.data.data)) {
          fetchedListings = listingsRes.data.data;
          setListings(fetchedListings);
        }

        if (fetchedListings.length > 0) {
          fetchPriceHistory(fetchedListings[0].listingId);
        }
      } catch (error) {
        toast.error("Failed to load product details");
      } finally {
        setLoading(false);
      }
    };

    fetchProductData();
  }, [productId]);

  const fetchPriceHistory = async (listingId: string) => {
    try {
      const res = await apiClient.get(`/v1/price-history`, {
        params: { listingId }
      });
      
      if (res.data?.success && Array.isArray(res.data.data)) {
        setPriceHistory(res.data.data);
      }
    } catch (error) {
      console.error("Failed to fetch price history");
    }
  };

  const filterPriceHistoryByRange = (history: PriceHistoryPoint[], range: TimeRange): PriceHistoryPoint[] => {
    if (range === "all") return history;
    
    const now = new Date();
    const daysMap = { "7d": 7, "30d": 30, "90d": 90 };
    const cutoffDate = new Date(now.getTime() - daysMap[range] * 24 * 60 * 60 * 1000);
    
    return history.filter(point => new Date(point.recordedAt) >= cutoffDate);
  };

  const filteredHistory = filterPriceHistoryByRange(priceHistory, selectedRange);

  const lowestListing = listings.reduce((lowest, current) => 
    (current.currentPrice < (lowest?.currentPrice ?? Infinity)) ? current : lowest
  , listings[0]);

  const handleSubscribe = async () => {
    if (!productId || !product) return;

    try {
      setSubmitting(true);
      
      const payload = {
        productId,
        targetPrice,
        notifyEmail,
        listingId: lowestListing?.listingId
      };

      const res = await apiClient.post("/v1/tracking", payload);
      
      if (res.data?.success) {
        setShowSuccessModal(true);
        toast.success("Price alert created successfully!");
      }
    } catch (error) {
      toast.error("Failed to create price alert");
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="container mx-auto max-w-7xl px-4 py-8">
        <div className="animate-pulse">
          <div className="h-96 bg-surface/50 rounded-3xl mb-8" />
          <div className="h-8 bg-surface/50 rounded w-1/2 mb-4" />
          <div className="h-4 bg-surface/50 rounded w-3/4 mb-8" />
        </div>
      </div>
    );
  }

  if (!product) {
    return (
      <div className="container mx-auto max-w-7xl px-4 py-8">
        <div className="text-center text-text-secondary">
          <p>Product not found</p>
          <Link to="/products" className="text-primary hover:underline mt-4 inline-block">
            Back to catalog
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto max-w-7xl px-4 py-8 relative z-10">
      {/* Success Modal */}
      {showSuccessModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
          <div className="hp-glass-card p-8 max-w-md w-full mx-4 text-center fade-in">
            <div className="w-20 h-20 mx-auto mb-6 rounded-full bg-success/20 flex items-center justify-center">
              <Check className="w-10 h-10 text-success" />
            </div>
            <h3 className="text-2xl font-display font-bold text-white mb-2">
              Alert Created!
            </h3>
            <p className="text-text-secondary mb-6">
              We'll notify you when the price drops below {formatPrice(targetPrice, product.currency)}
            </p>
            <button
              onClick={() => setShowSuccessModal(false)}
              className="btn-ieee bg-primary text-white px-8 py-3 rounded-full font-semibold"
            >
              Got it
            </button>
          </div>
        </div>
      )}

      {/* Back Button */}
      <Link 
        to="/products"
        className="inline-flex items-center gap-2 text-text-secondary hover:text-white transition mb-6 reveal"
      >
        <ArrowLeft className="w-4 h-4" />
        <span className="text-sm font-medium">Back to catalog</span>
      </Link>

      {/* Hero Section with Ken Burns Effect */}
      <div className="relative rounded-3xl overflow-hidden mb-8 h-[500px] reveal" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
        <div className="absolute inset-0">
          <img
            src={product.primaryImage || "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?auto=format&fit=crop&w=1200&q=80"}
            alt={product.name}
            className="w-full h-full object-cover"
          />
        </div>
        <div className="absolute inset-0 bg-gradient-to-t from-surface via-surface/60 to-transparent" />
        
        {/* Badges */}
        <div className="absolute top-6 left-6 flex gap-3">
          {product.brand && (
            <div className="reveal-visible animate-slide-in-left" style={{ animationDelay: "200ms" }}>
              <span className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-surface/80 backdrop-blur-md border border-white/10 text-white text-sm font-medium">
                <Sparkles className="w-4 h-4 text-accent" />
                {product.brand}
              </span>
            </div>
          )}
          {product.category && (
            <div className="reveal-visible animate-slide-in-left" style={{ animationDelay: "300ms" }}>
              <span className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-surface/80 backdrop-blur-md border border-white/10 text-white text-sm font-medium">
                {product.category}
              </span>
            </div>
          )}
        </div>

        {/* Lowest Price Badge */}
        {product.lowestPrice && (
          <div className="absolute top-6 right-6 reveal-visible animate-slide-in-right" style={{ animationDelay: "400ms" }}>
            <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-success/20 backdrop-blur-md border border-success/30 text-success text-sm font-bold">
              <TrendingDown className="w-4 h-4" />
              Lowest: {formatPrice(product.lowestPrice, product.currency)}
            </div>
          </div>
        )}

        {/* Product Info */}
        <div className="absolute bottom-0 left-0 right-0 p-8">
          <div className="reveal-visible animate-slide-in-up" style={{ animationDelay: "500ms" }}>
            <h1 className="text-4xl md:text-5xl font-display font-black text-white mb-3">
              {product.name}
            </h1>
            {product.description && (
              <p className="text-text-secondary text-lg max-w-2xl">
                {product.description}
              </p>
            )}
          </div>
        </div>
      </div>

      {/* Store Comparison Table */}
      <div className="mb-8 reveal" style={{ "--reveal-delay": "200ms" } as React.CSSProperties}>
        <h2 className="text-2xl font-display font-bold text-text-primary mb-6 flex items-center gap-3">
          <Store className="w-6 h-6 text-primary" />
          Store Comparison
        </h2>
        
        {listings.length === 0 ? (
          <div className="hp-glass-card p-8 text-center text-text-secondary">
            <Store className="w-12 h-12 mx-auto mb-4 text-text-muted" />
            <p>No store listings available for this product</p>
          </div>
        ) : (
          <div className="hp-glass-card overflow-hidden">
            <table className="w-full">
              <thead>
                <tr className="border-b border-white/10">
                  <th className="text-left p-4 text-text-secondary font-medium">Store</th>
                  <th className="text-left p-4 text-text-secondary font-medium">Current Price</th>
                  <th className="text-left p-4 text-text-secondary font-medium">Last Updated</th>
                  <th className="text-left p-4 text-text-secondary font-medium">Status</th>
                  <th className="text-right p-4 text-text-secondary font-medium">Action</th>
                </tr>
              </thead>
              <tbody>
                {listings.map((listing) => {
                  const isLowest = listing.listingId === lowestListing?.listingId;
                  return (
                    <tr 
                      key={listing.listingId}
                      className={`border-b border-white/5 transition ${
                        isLowest ? "bg-success/5 pulse-border" : "hover:bg-white/5"
                      }`}
                    >
                      <td className="p-4">
                        <div className="flex items-center gap-3">
                          <div className="w-10 h-10 rounded-full bg-surface flex items-center justify-center">
                            <Store className="w-5 h-5 text-text-secondary" />
                          </div>
                          <span className="font-medium text-text-primary">{listing.storeName}</span>
                        </div>
                      </td>
                      <td className="p-4">
                        <span className={`font-mono font-bold ${isLowest ? "text-success text-lg" : "text-text-primary"}`}>
                          {formatPrice(listing.currentPrice, listing.currency)}
                        </span>
                        {isLowest && (
                          <span className="ml-2 text-xs text-success font-semibold">LOWEST</span>
                        )}
                      </td>
                      <td className="p-4 text-text-secondary text-sm">
                        {new Date(listing.lastScrapedAt).toLocaleDateString()}
                      </td>
                      <td className="p-4">
                        <span className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold ${
                          listing.isActive 
                            ? "bg-success/20 text-success" 
                            : "bg-warning/20 text-warning"
                        }`}>
                          <span className={`w-2 h-2 rounded-full ${listing.isActive ? "bg-success" : "bg-warning"}`} />
                          {listing.isActive ? "Active" : "Inactive"}
                        </span>
                      </td>
                      <td className="p-4 text-right">
                        {listing.storeUrl && (
                          <a
                            href={listing.storeUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="btn-ieee bg-primary/20 hover:bg-primary text-primary hover:text-white px-4 py-1.5 rounded-full text-xs font-semibold inline-flex items-center gap-1.5 transition cursor-pointer"
                          >
                            Go to Store
                            <ExternalLink className="w-3.5 h-3.5" />
                          </a>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Price History Chart */}
      <div className="mb-8 reveal" style={{ "--reveal-delay": "300ms" } as React.CSSProperties}>
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-display font-bold text-text-primary flex items-center gap-3">
            <TrendingDown className="w-6 h-6 text-accent" />
            Price History
          </h2>
          <div className="flex items-center gap-4">
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
            <Link
              to={`/products/${productId}/history`}
              className="text-sm text-primary hover:text-primary-light transition"
            >
              View detailed history →
            </Link>
          </div>
        </div>

        {filteredHistory.length === 0 ? (
          <div className="hp-glass-card p-8 text-center text-text-secondary">
            <TrendingDown className="w-12 h-12 mx-auto mb-4 text-text-muted" />
            <p>No price history available for this time range</p>
          </div>
        ) : (
          <div className="hp-glass-card p-6">
            <PriceHistoryChart 
              dataPoints={filteredHistory.map(point => ({
                ...point,
                price: convertPrice(point.price, product.currency) ?? point.price
              }))} 
              currencyCode={activeCurrency} 
            />
          </div>
        )}
      </div>

      {/* Alert Setup Card */}
      <div className="mb-8 reveal" style={{ "--reveal-delay": "400ms" } as React.CSSProperties}>
        <h2 className="text-2xl font-display font-bold text-text-primary mb-6 flex items-center gap-3">
          <Bell className="w-6 h-6 text-warning" />
          Set Price Alert
        </h2>

        <div className="hp-glass-card p-6">
          <div className="grid md:grid-cols-2 gap-8">
            {/* Target Price Slider */}
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-4">
                Target Price: {formatPrice(targetPrice, product.currency)}
              </label>
              <input
                type="range"
                min="0"
                max={product.lowestPrice ? product.lowestPrice * 1.5 : 1000}
                step="1"
                value={targetPrice}
                onChange={(e) => setTargetPrice(Number(e.target.value))}
                className="w-full h-2 bg-surface rounded-lg appearance-none cursor-pointer accent-primary"
              />
              <div className="flex justify-between mt-2 text-xs text-text-muted">
                <span>{formatPrice(0, product.currency)}</span>
                <span>{formatPrice(product.lowestPrice ? product.lowestPrice * 1.5 : 1000, product.currency)}</span>
              </div>
            </div>

            {/* Email Toggle */}
            <div className="flex items-center justify-between">
              <div>
                <p className="font-medium text-text-primary">Email Notifications</p>
                <p className="text-sm text-text-secondary">Receive email when price drops</p>
              </div>
              <button
                onClick={() => setNotifyEmail(!notifyEmail)}
                className={`relative w-14 h-8 rounded-full transition-colors ${
                  notifyEmail ? "bg-primary" : "bg-surface"
                }`}
              >
                <span
                  className={`absolute top-1 left-1 w-6 h-6 rounded-full bg-white transition-transform ${
                    notifyEmail ? "translate-x-6" : "translate-x-0"
                  }`}
                />
              </button>
            </div>
          </div>

          <button
            onClick={handleSubscribe}
            disabled={submitting}
            className="btn-ieee w-full mt-6 bg-primary text-white py-4 rounded-2xl font-semibold flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {submitting ? (
              <>
                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                Creating alert...
              </>
            ) : (
              <>
                <Bell className="w-5 h-5" />
                Create Price Alert
              </>
            )}
          </button>
        </div>
      </div>

      <style>{`
        @keyframes slideInLeft {
          from {
            opacity: 0;
            transform: translateX(-30px);
          }
          to {
            opacity: 1;
            transform: translateX(0);
          }
        }

        @keyframes slideInRight {
          from {
            opacity: 0;
            transform: translateX(30px);
          }
          to {
            opacity: 1;
            transform: translateX(0);
          }
        }

        @keyframes slideInUp {
          from {
            opacity: 0;
            transform: translateY(30px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }

        @keyframes scaleIn {
          from {
            opacity: 0;
            transform: scale(0.9);
          }
          to {
            opacity: 1;
            transform: scale(1);
          }
        }

        .animate-slide-in-left {
          animation: slideInLeft 0.6s cubic-bezier(0.23, 1, 0.32, 1) forwards;
        }

        .animate-slide-in-right {
          animation: slideInRight 0.6s cubic-bezier(0.23, 1, 0.32, 1) forwards;
        }

        .animate-slide-in-up {
          animation: slideInUp 0.6s cubic-bezier(0.23, 1, 0.32, 1) forwards;
        }

        .animate-scale-in {
          animation: scaleIn 0.3s cubic-bezier(0.23, 1, 0.32, 1) forwards;
        }

        .pulse-border {
          animation: pulseBorder 2s ease-in-out infinite;
        }

        @keyframes pulseBorder {
          0%, 100% {
            box-shadow: inset 0 0 0 2px rgba(0, 230, 118, 0.3);
          }
          50% {
            box-shadow: inset 0 0 0 4px rgba(0, 230, 118, 0.5);
          }
        }
      `}</style>
    </div>
  );
}
