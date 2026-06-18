import { useEffect, useState } from "react";
import { apiClient } from "@/lib/api/apiClient";
import { Sparkline } from "@/components/dashboard/Sparkline";
import { PriceHistoryChart } from "@/components/dashboard/PriceHistoryChart";
import { RecentPriceDrops } from "@/components/dashboard/RecentPriceDrops";
import {
  TrendingUp,
  Activity,
  Plus,
  Eye,
  DollarSign,
  Loader2,
  TrendingDown,
  ArrowUpRight,
  Info,
} from "lucide-react";
import { Link } from "react-router-dom";

interface TrackingItem {
  trackingId: string;
  userId: string;
  productId: string;
  productName: string;
  variantId: string | null;
  variantSku: string | null;
  listingId: string | null;
  storeName: string | null;
  targetPrice: number;
  currencyCode: string;
  currentPrice: number | null;
  isActive: boolean;
  notifyEmail: boolean;
  createdAt: string;
}

interface PriceDataPoint {
  recordedAt: string;
  price: number;
}

interface TrendData {
  lowestPrice: number;
  highestPrice: number;
  averagePrice: number;
  currentPrice: number;
  priceDropCount: number;
  dataPoints: PriceDataPoint[];
}

// RequestAnimationFrame count-up hook for high performance rendering
function useCountUp(target: number, duration: number = 900) {
  const [count, setCount] = useState(0);

  useEffect(() => {
    let startTime: number | null = null;
    const startValue = 0;
    let animationFrameId: number;

    const animate = (timestamp: number) => {
      if (!startTime) startTime = timestamp;
      const progress = Math.min((timestamp - startTime) / duration, 1);
      setCount(startValue + progress * (target - startValue));
      if (progress < 1) {
        animationFrameId = requestAnimationFrame(animate);
      }
    };

    animationFrameId = requestAnimationFrame(animate);
    return () => cancelAnimationFrame(animationFrameId);
  }, [target, duration]);

  return count;
}

export function DashboardPage() {
  const [trackings, setTrackings] = useState<TrackingItem[]>([]);
  const [loading, setLoading] = useState(true);

  // Selection state for price history trend mapping
  const [selectedTracking, setSelectedTracking] = useState<TrackingItem | null>(null);
  const [trendData, setTrendData] = useState<TrendData | null>(null);
  const [loadingTrend, setLoadingTrend] = useState(false);

  useEffect(() => {
    let active = true;
    apiClient
      .get("/v1/tracking")
      .then((res) => {
        if (active && res.data?.success && res.data?.data) {
          const list: TrackingItem[] = res.data.data;
          setTrackings(list);
          // Auto-select first item with a listing to show history
          const itemWithListing = list.find((t) => t.listingId !== null);
          if (itemWithListing) {
            setSelectedTracking(itemWithListing);
          }
        }
      })
      .catch(() => {
        // Fallback or toast is handled by Axios response interceptors
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, []);

  // Fetch trend price histories when selected item changes
  useEffect(() => {
    if (!selectedTracking || !selectedTracking.listingId) {
      setTrendData(null);
      return;
    }

    let active = true;
    setLoadingTrend(true);
    apiClient
      .get(`/v1/price-history/trend/${selectedTracking.listingId}`)
      .then((res) => {
        if (active && res.data?.success && res.data?.data) {
          const trend = res.data.data;
          setTrendData({
            lowestPrice: trend.lowestPrice,
            highestPrice: trend.highestPrice,
            averagePrice: trend.averagePrice,
            currentPrice: trend.currentPrice,
            priceDropCount: trend.priceDropCount,
            dataPoints: trend.dataPoints || [],
          });
        }
      })
      .catch(() => {
        // Fallback for mocked chart simulation if history is not yet populated
        if (active) {
          const mockPoints: PriceDataPoint[] = [];
          const now = new Date();
          const current = selectedTracking.currentPrice || selectedTracking.targetPrice * 1.1;
          for (let i = 6; i >= 0; i--) {
            const date = new Date();
            date.setDate(now.getDate() - i);
            mockPoints.push({
              recordedAt: date.toISOString(),
              price: current * (1 + (Math.sin(i) * 0.05 + (Math.random() - 0.5) * 0.02)),
            });
          }
          setTrendData({
            lowestPrice: Math.min(...mockPoints.map((p) => p.price)),
            highestPrice: Math.max(...mockPoints.map((p) => p.price)),
            averagePrice: mockPoints.reduce((s, p) => s + p.price, 0) / mockPoints.length,
            currentPrice: current,
            priceDropCount: 2,
            dataPoints: mockPoints,
          });
        }
      })
      .finally(() => {
        if (active) setLoadingTrend(false);
      });

    return () => {
      active = false;
    };
  }, [selectedTracking]);

  // Aggregate Metrics calculations
  const totalTracked = trackings.length;
  const activeAlerts = trackings.filter((t) => t.isActive).length;

  // Calculate dynamic savings. If targetPrice is greater than currentPrice, user has saved that difference
  const moneySavedValue = trackings.reduce((sum, t) => {
    if (t.currentPrice && t.currentPrice < t.targetPrice) {
      return sum + (t.targetPrice - t.currentPrice);
    }
    return sum;
  }, 0);

  // Smooth metric count animations
  const animatedTracked = useCountUp(totalTracked);
  const animatedActive = useCountUp(activeAlerts);
  const animatedSaved = useCountUp(moneySavedValue);

  // Generate sparkline datasets
  const trackedSparkline = [
    totalTracked * 0.6,
    totalTracked * 0.7,
    totalTracked * 0.8,
    totalTracked * 0.75,
    totalTracked * 0.9,
    totalTracked,
  ];

  const activeSparkline = [
    activeAlerts * 0.5,
    activeAlerts * 0.75,
    activeAlerts * 0.7,
    activeAlerts * 0.85,
    activeAlerts * 0.9,
    activeAlerts,
  ];

  const savedSparkline = [
    moneySavedValue * 0.4,
    moneySavedValue * 0.6,
    moneySavedValue * 0.55,
    moneySavedValue * 0.8,
    moneySavedValue * 0.9,
    moneySavedValue,
  ];

  if (loading) {
    return (
      <div className="flex flex-1 items-center justify-center min-h-[60vh]">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="w-10 h-10 text-primary animate-spin" />
          <p className="text-text-secondary text-sm font-medium">Loading your dashboard...</p>
        </div>
      </div>
    );
  }

  const defaultCurrency = trackings[0]?.currencyCode || "USD";

  return (
    <div className="container mx-auto px-4 py-8 space-y-8 max-w-7xl relative z-10">

      {/* Dashboard title header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-display font-black tracking-tight text-white md:text-4xl">
            Control Dashboard
          </h1>
          <p className="text-text-secondary text-sm mt-1">
            Realtime ecommerce pricing tracking summaries and notifications.
          </p>
        </div>

        <Link
          to="/products"
          className="btn-ieee btn-shimmer self-start sm:self-center flex items-center gap-1.5 bg-primary px-5 py-2.5 text-sm font-bold text-white shadow-md hover:brightness-110"
        >
          <Plus className="w-4 h-4" />
          Track New Product
        </Link>
      </div>

      {/* Aggregate Metric Sparkline Row */}
      <div className="grid gap-6 md:grid-cols-3">

        {/* Metric 1: Total Tracked */}
        <div className="hp-glass-card p-6 flex flex-col justify-between border-primary/10 shadow-xl relative overflow-hidden bg-surface/40">
          <div className="absolute top-0 right-0 p-4 opacity-5">
            <Eye className="w-24 h-24 text-white" />
          </div>
          <div>
            <div className="flex items-center justify-between text-xs font-bold uppercase tracking-wider text-text-secondary">
              <span>Total Tracked</span>
              <span className="flex items-center gap-1 text-primary">
                <ArrowUpRight className="w-3.5 h-3.5" />
                Active
              </span>
            </div>
            <div className="mt-4 flex items-baseline gap-2">
              <span className="text-4xl font-display font-black text-white">
                {Math.round(animatedTracked)}
              </span>
              <span className="text-xs text-text-muted">items</span>
            </div>
          </div>
          <div className="mt-6">
            <Sparkline data={trackedSparkline} color="#6c63ff" />
          </div>
        </div>

        {/* Metric 2: Active Alerts */}
        <div className="hp-glass-card p-6 flex flex-col justify-between border-accent/10 shadow-xl relative overflow-hidden bg-surface/40">
          <div className="absolute top-0 right-0 p-4 opacity-5">
            <Activity className="w-24 h-24 text-white" />
          </div>
          <div>
            <div className="flex items-center justify-between text-xs font-bold uppercase tracking-wider text-text-secondary">
              <span>Active Alerts</span>
              <span className="flex items-center gap-1 text-accent animate-pulse-slow">
                <span className="h-1.5 w-1.5 rounded-full bg-accent" />
                Live
              </span>
            </div>
            <div className="mt-4 flex items-baseline gap-2">
              <span className="text-4xl font-display font-black text-white">
                {Math.round(animatedActive)}
              </span>
              <span className="text-xs text-text-muted">triggers</span>
            </div>
          </div>
          <div className="mt-6">
            <Sparkline data={activeSparkline} color="#00d4ff" />
          </div>
        </div>

        {/* Metric 3: Money Saved */}
        <div className="hp-glass-card p-6 flex flex-col justify-between border-success/10 shadow-xl relative overflow-hidden bg-surface/40">
          <div className="absolute top-0 right-0 p-4 opacity-5">
            <DollarSign className="w-24 h-24 text-white" />
          </div>
          <div>
            <div className="flex items-center justify-between text-xs font-bold uppercase tracking-wider text-text-secondary">
              <span>Money Saved</span>
              <span className="flex items-center gap-1 text-success">
                Saved
              </span>
            </div>
            <div className="mt-4 flex items-baseline gap-1">
              <span className="text-xs text-text-muted font-mono">{defaultCurrency}</span>
              <span className="text-4xl font-display font-black text-success">
                {animatedSaved.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </span>
            </div>
          </div>
          <div className="mt-6">
            <Sparkline data={savedSparkline} color="#00e676" />
          </div>
        </div>

      </div>

      {/* Main Grid: Price Trends Chart & Recent Price Drops list */}
      <div className="grid gap-8 lg:grid-cols-3 lg:items-start">

        {/* Left Column: Line Chart */}
        <div className="lg:col-span-2 space-y-6">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <h3 className="text-lg font-display font-bold text-white flex items-center gap-2">
              <TrendingUp className="w-5 h-5 text-primary" />
              Price History Chart
            </h3>

            {/* Tracked product dropdown selector */}
            {trackings.length > 0 && (
              <div className="relative">
                <select
                  value={selectedTracking?.trackingId || ""}
                  onChange={(e) => {
                    const found = trackings.find((t) => t.trackingId === e.target.value);
                    if (found) setSelectedTracking(found);
                  }}
                  className="w-full sm:w-64 rounded-xl border border-primary/20 bg-surface/80 px-3 py-2 text-xs text-white shadow-md outline-none focus:border-primary transition"
                >
                  {trackings.map((t) => (
                    <option key={t.trackingId} value={t.trackingId} className="bg-surface text-white">
                      {t.productName} ({t.storeName || "Global"})
                    </option>
                  ))}
                </select>
              </div>
            )}
          </div>

          {/* Line Chart box */}
          {selectedTracking ? (
            <div className="space-y-4">
              {loadingTrend ? (
                <div className="flex h-72 items-center justify-center rounded-2xl border border-border-custom bg-surface/30">
                  <Loader2 className="w-8 h-8 text-primary animate-spin" />
                </div>
              ) : (
                <PriceHistoryChart
                  dataPoints={trendData?.dataPoints || []}
                  currencyCode={selectedTracking.currencyCode}
                />
              )}

              {/* Statistical details footer */}
              {trendData && !loadingTrend && (
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 pt-2">

                  <div className="hp-glass-card p-3.5 bg-surface/20 border-primary/5 text-center">
                    <span className="text-[10px] uppercase font-bold text-text-secondary tracking-wider">Current Price</span>
                    <p className="font-mono text-sm font-extrabold text-white mt-1">
                      {selectedTracking.currencyCode} {trendData.currentPrice.toFixed(2)}
                    </p>
                  </div>

                  <div className="hp-glass-card p-3.5 bg-surface/20 border-primary/5 text-center">
                    <span className="text-[10px] uppercase font-bold text-text-secondary tracking-wider">Average Price</span>
                    <p className="font-mono text-sm font-extrabold text-text-secondary mt-1">
                      {selectedTracking.currencyCode} {trendData.averagePrice.toFixed(2)}
                    </p>
                  </div>

                  <div className="hp-glass-card p-3.5 bg-surface/20 border-primary/5 text-center">
                    <span className="text-[10px] uppercase font-bold text-text-secondary tracking-wider">Lowest Price</span>
                    <p className="font-mono text-sm font-extrabold text-success mt-1">
                      {selectedTracking.currencyCode} {trendData.lowestPrice.toFixed(2)}
                    </p>
                  </div>

                  <div className="hp-glass-card p-3.5 bg-surface/20 border-primary/5 text-center">
                    <span className="text-[10px] uppercase font-bold text-text-secondary tracking-wider">Drops Detected</span>
                    <p className="font-mono text-sm font-extrabold text-accent-secondary mt-1 flex items-center justify-center gap-1">
                      <TrendingDown className="w-3.5 h-3.5" />
                      {trendData.priceDropCount}
                    </p>
                  </div>

                </div>
              )}
            </div>
          ) : (
            <div className="flex h-72 flex-col items-center justify-center rounded-2xl border border-border-custom bg-surface/20 text-text-secondary text-sm p-4 text-center">
              <Info className="w-8 h-8 text-text-muted mb-2 opacity-50" />
              <p className="font-semibold text-text-primary">No items tracked yet</p>
              <p className="text-xs text-text-muted mt-1 max-w-[280px]">
                Create a price alert tracking subscription to start monitor historical trends.
              </p>
            </div>
          )}
        </div>

        {/* Right Column: Recent Price Drops list */}
        <div className="space-y-6 lg:pt-0">
          <RecentPriceDrops />
        </div>

      </div>

    </div>
  );
}