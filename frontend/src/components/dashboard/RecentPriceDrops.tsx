import { useEffect, useState } from "react";
import { apiClient } from "@/lib/api/apiClient";
import { TrendingDown, Calendar, ShoppingCart, Loader2 } from "lucide-react";

interface NotificationItem {
  notificationId: number;
  productName: string;
  variantSku: string | null;
  storeName: string;
  triggeredPrice: number;
  targetPrice: number;
  currencyCode: string;
  sentAt: string;
  status: number;
}

export function RecentPriceDrops() {
  const [drops, setDrops] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    apiClient
      .get("/v1/notifications", { params: { page: 0, size: 5 } })
      .then((res) => {
        if (active && res.data?.success && res.data?.data?.items) {
          setDrops(res.data.data.items);
        }
      })
      .catch(() => {
        // Fallback gracefully on fetch failures
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, []);

  const content = loading ? (
    <div className="flex h-48 items-center justify-center rounded-2xl border border-border-custom bg-surface/30">
      <Loader2 className="w-6 h-6 text-primary animate-spin" />
    </div>
  ) : drops.length === 0 ? (
    <div className="flex h-48 flex-col items-center justify-center rounded-2xl border border-border-custom bg-surface/30 text-text-secondary text-sm p-4 text-center">
      <TrendingDown className="w-8 h-8 text-text-muted mb-2 opacity-50" />
      <p className="font-semibold text-text-primary">No recent price drops</p>
      <p className="text-xs text-text-muted mt-1 max-w-[240px]">
        Price drops will appear here as soon as the scraper detects discount drops.
      </p>
    </div>
  ) : (
    <div className="grid gap-3.5">
      {drops.map((drop, i) => {
        const discountPct = Math.round(
          ((drop.targetPrice - drop.triggeredPrice) / drop.targetPrice) * 100
        );

        return (
          <div
            key={drop.notificationId}
            className="group hp-glass-card p-4 flex items-center justify-between gap-4 card-hover-lift border-primary/10 hover:border-primary/30"
            style={{
              opacity: 0,
              transform: "translateX(40px)",
              animation: "slideInRightToLeft 0.6s cubic-bezier(0.16, 1, 0.3, 1) forwards",
              animationDelay: `${i * 120}ms`,
            }}
          >
            <div className="flex items-center gap-3.5 min-w-0">
              <div className="flex-shrink-0 h-10 w-10 rounded-xl bg-accent-secondary/10 border border-accent-secondary/20 flex items-center justify-center text-accent-secondary transition-colors group-hover:bg-accent-secondary/20">
                <TrendingDown className="w-5 h-5" />
              </div>
              <div className="min-w-0">
                <h4 className="text-sm font-semibold text-white truncate max-w-[200px] sm:max-w-[320px]">
                  {drop.productName}
                </h4>
                <div className="flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-text-secondary mt-1">
                  <span className="flex items-center gap-1 rounded bg-white/5 px-1.5 py-0.5 font-medium">
                    <ShoppingCart className="w-3 h-3 text-accent" />
                    {drop.storeName}
                  </span>
                  {drop.variantSku && (
                    <span className="font-mono text-[10px] text-text-muted">
                      SKU: {drop.variantSku}
                    </span>
                  )}
                  <span className="flex items-center gap-1 text-text-muted">
                    <Calendar className="w-3 h-3" />
                    {new Date(drop.sentAt).toLocaleDateString()}
                  </span>
                </div>
              </div>
            </div>

            <div className="flex flex-col items-end gap-1.5 flex-shrink-0">
              <div className="text-right">
                <span className="text-xs text-text-muted line-through mr-1.5 font-mono">
                  {drop.currencyCode} {drop.targetPrice.toFixed(2)}
                </span>
                <span className="text-sm font-black text-success font-mono">
                  {drop.currencyCode} {drop.triggeredPrice.toFixed(2)}
                </span>
              </div>
              {discountPct > 0 && (
                <span className="rounded-full bg-success/10 border border-success/20 px-2 py-0.5 text-[10px] font-extrabold text-success uppercase tracking-wider animate-pulse-slow">
                  -{discountPct}% Off Target
                </span>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );

  return (
    <div className="space-y-6">
      <h3 className="text-lg font-display font-bold text-white flex items-center gap-2">
        <TrendingDown className="w-5 h-5 text-accent-secondary" />
        Recent Price Drops
      </h3>
      {content}
      <style>{`
        @keyframes slideInRightToLeft {
          to {
            opacity: 1;
            transform: translateX(0);
          }
        }
      `}</style>
    </div>
  );
}