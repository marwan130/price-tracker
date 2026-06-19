import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiClient } from "@/lib/api/apiClient";
import { Edit2, Trash2, Target, Check, Loader2, XCircle } from "lucide-react";
import toast from "react-hot-toast";

interface TrackingItem {
  trackingId: string;
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

export function ActiveTrackingsPage() {
  const [trackings, setTrackings] = useState<TrackingItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [editPrice, setEditPrice] = useState<number>(0);
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [deletingIds, setDeletingIds] = useState<Set<string>>(new Set());
  const [showModal, setShowModal] = useState(false);
  const [modalTracking, setModalTracking] = useState<TrackingItem | null>(null);

  useEffect(() => {
    let active = true;
    apiClient
      .get("/v1/tracking")
      .then((res) => {
        if (active && res.data?.success && res.data?.data) {
          setTrackings(res.data.data);
        }
      })
      .catch(() => {
        toast.error("Failed to load trackings");
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, []);

  const handleEdit = (tracking: TrackingItem) => {
    setModalTracking(tracking);
    setEditPrice(tracking.targetPrice);
    setShowModal(true);
  };

  const handleSaveEdit = async () => {
    if (!modalTracking) return;
    
    try {
      setSaving(true);
      const res = await apiClient.put(`/v1/tracking/${modalTracking.trackingId}`, {
        targetPrice: editPrice,
      });

      if (res.data?.success) {
        setTrackings(trackings.map(t => 
          t.trackingId === modalTracking.trackingId ? { ...t, targetPrice: editPrice } : t
        ));
        toast.success("Target price updated");
        setShowModal(false);
        setModalTracking(null);
      }
    } catch (error) {
      toast.error("Failed to update target price");
    } finally {
      setSaving(false);
    }
  };

  const handleCancelEdit = () => {
    setShowModal(false);
    setModalTracking(null);
    setEditPrice(0);
  };

  const handleDelete = async (trackingId: string) => {
    try {
      setDeletingId(trackingId);
      setDeletingIds(prev => new Set(prev).add(trackingId));
      
      // Wait for animation to complete
      await new Promise(resolve => setTimeout(resolve, 300));
      
      const res = await apiClient.delete(`/v1/tracking/${trackingId}`);

      if (res.data?.success) {
        setTrackings(trackings.filter(t => t.trackingId !== trackingId));
        toast.success("Tracking deleted");
      }
    } catch (error) {
      toast.error("Failed to delete tracking");
      setDeletingIds(prev => {
        const newSet = new Set(prev);
        newSet.delete(trackingId);
        return newSet;
      });
    } finally {
      setDeletingId(null);
      setDeletingIds(prev => {
        const newSet = new Set(prev);
        newSet.delete(trackingId);
        return newSet;
      });
    }
  };

  const calculateProgress = (target: number, current: number | null) => {
    if (!current) return 0;
    if (current <= target) return 100;
    const total = current * 1.5;
    return Math.max(0, Math.min(100, ((total - current) / (total - target)) * 100));
  };

  const isTargetReached = (target: number, current: number | null) => {
    return current !== null && current <= target;
  };

  if (loading) {
    return (
      <div className="container mx-auto max-w-7xl px-4 py-8">
        <div className="flex flex-1 items-center justify-center min-h-[60vh]">
          <div className="flex flex-col items-center gap-3">
            <Loader2 className="w-10 h-10 text-primary animate-spin" />
            <p className="text-text-secondary text-sm font-medium">Loading trackings...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto max-w-7xl px-4 py-8 space-y-8">
      {/* Header */}
      <div className="reveal">
        <h1 className="text-3xl font-display font-black tracking-tight text-white md:text-4xl">
          Active Trackings
        </h1>
        <p className="text-text-secondary text-sm mt-1">
          Manage your price alert subscriptions and target thresholds.
        </p>
      </div>

      {/* Trackings Grid */}
      {trackings.length === 0 ? (
        <div className="hp-glass-card p-16 text-center relative overflow-hidden reveal" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
          {/* Floating animated elements */}
          <div className="absolute inset-0 pointer-events-none">
            <div className="absolute top-10 left-10 w-20 h-20 rounded-full bg-primary/10 animate-float" style={{ animationDelay: '0s' }} />
            <div className="absolute top-20 right-20 w-16 h-16 rounded-full bg-accent/10 animate-float" style={{ animationDelay: '1s' }} />
            <div className="absolute bottom-20 left-1/4 w-24 h-24 rounded-full bg-success/10 animate-float" style={{ animationDelay: '2s' }} />
            <div className="absolute bottom-10 right-1/3 w-12 h-12 rounded-full bg-warning/10 animate-float" style={{ animationDelay: '1.5s' }} />
          </div>
          
          <div className="relative z-10">
            <div className="w-24 h-24 mx-auto mb-6 rounded-full bg-surface/50 flex items-center justify-center animate-scale-in">
              <Target className="w-12 h-12 text-text-muted" />
            </div>
            <h3 className="text-2xl font-display font-bold text-white mb-3">No active trackings</h3>
            <p className="text-text-secondary mb-6 max-w-md mx-auto">
              Start tracking products to get price drop alerts. Browse the product catalog to find items you want to monitor.
            </p>
            <Link
              to="/products"
              className="btn-ieee inline-flex bg-primary px-6 py-3 rounded-full text-white font-semibold hover:brightness-110 transition"
            >
              Browse Products
            </Link>
          </div>
        </div>
      ) : (
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {trackings.map((tracking, index) => {
            const progress = calculateProgress(tracking.targetPrice, tracking.currentPrice);
            const targetReached = isTargetReached(tracking.targetPrice, tracking.currentPrice);
            
            return (
              <div
                key={tracking.trackingId}
                className={`reveal hp-glass-card p-6 relative overflow-hidden ${
                  deletingIds.has(tracking.trackingId) ? 'animate-delete' : ''
                }`}
                style={{ "--reveal-delay": `${(index + 1) * 100}ms` } as React.CSSProperties}
              >
                {/* Progress bar background */}
                <div 
                  className="absolute inset-0 opacity-10 transition-colors"
                  style={{
                    background: targetReached 
                      ? 'linear-gradient(180deg, rgba(0, 230, 118, 0.2) 0%, transparent 100%)'
                      : 'linear-gradient(180deg, rgba(108, 99, 255, 0.2) 0%, transparent 100%)'
                  }}
                />

                {/* Product Info */}
                <div className="relative z-10">
                  <h3 className="font-bold text-white text-lg mb-1 line-clamp-2">
                    {tracking.productName}
                  </h3>
                  <p className="text-text-secondary text-sm mb-4">
                    {tracking.storeName || "Global"}
                  </p>

                  {/* Price Comparison */}
                  <div className="space-y-3 mb-4">
                    <div className="flex justify-between items-center">
                      <span className="text-text-secondary text-sm">Target Price</span>
                      <span className="font-mono font-bold text-white">
                        {tracking.currencyCode} {tracking.targetPrice.toFixed(2)}
                      </span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-text-secondary text-sm">Current Price</span>
                      <span className={`font-mono font-bold ${targetReached ? 'text-success' : 'text-white'}`}>
                        {tracking.currentPrice 
                          ? `${tracking.currencyCode} ${tracking.currentPrice.toFixed(2)}`
                          : 'N/A'
                        }
                      </span>
                    </div>
                  </div>

                  {/* Progress Bar */}
                  <div className="mb-4">
                    <div className="h-2 bg-surface rounded-full overflow-hidden">
                      <div
                        className={`h-full transition-all duration-500 ${
                          targetReached ? 'bg-success' : 'bg-primary'
                        }`}
                        style={{ width: `${progress}%` }}
                      />
                    </div>
                    <div className="flex justify-between mt-1">
                      <span className="text-xs text-text-muted">
                        {targetReached ? 'Target reached!' : `${Math.round(progress)}% to target`}
                      </span>
                      {targetReached && (
                        <span className="text-xs text-success font-semibold flex items-center gap-1">
                          <Check className="w-3 h-3" />
                          Alert triggered
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex gap-2 pt-4 border-t border-white/10">
                    <button
                      onClick={() => handleEdit(tracking)}
                      className="flex-1 flex items-center justify-center gap-1.5 px-3 py-2 rounded-xl bg-primary/20 text-primary text-sm font-semibold hover:bg-primary/30 transition"
                    >
                      <Edit2 className="w-4 h-4" />
                      Edit
                    </button>
                    <button
                      onClick={() => handleDelete(tracking.trackingId)}
                      disabled={deletingId === tracking.trackingId}
                      className="flex items-center justify-center px-3 py-2 rounded-xl bg-accent-secondary/20 text-accent-secondary text-sm font-semibold hover:bg-accent-secondary/30 transition disabled:opacity-50"
                    >
                      {deletingId === tracking.trackingId ? (
                        <Loader2 className="w-4 h-4 animate-spin" />
                      ) : (
                        <Trash2 className="w-4 h-4" />
                      )}
                    </button>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Edit Modal */}
      {showModal && modalTracking && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
          <div className="hp-glass-card p-8 max-w-md w-full mx-4 animate-scale-in">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-2xl font-display font-bold text-white">
                Edit Target Price
              </h3>
              <button
                onClick={handleCancelEdit}
                className="text-text-secondary hover:text-white transition"
              >
                <XCircle className="w-6 h-6" />
              </button>
            </div>

            <div className="mb-6">
              <p className="text-text-secondary text-sm mb-2">
                Product: {modalTracking.productName}
              </p>
              <p className="text-text-secondary text-sm mb-4">
                Store: {modalTracking.storeName || "Global"}
              </p>
              
              <label className="block text-sm font-medium text-text-secondary mb-2">
                New Target Price ({modalTracking.currencyCode})
              </label>
              <input
                type="number"
                value={editPrice}
                onChange={(e) => setEditPrice(Number(e.target.value))}
                className="w-full hp-input"
                step="0.01"
                min="0"
              />
            </div>

            <div className="flex gap-3">
              <button
                onClick={handleCancelEdit}
                disabled={saving}
                className="flex-1 flex items-center justify-center gap-1.5 px-4 py-3 rounded-xl bg-white/10 text-white text-sm font-semibold hover:bg-white/20 transition disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                onClick={handleSaveEdit}
                disabled={saving}
                className="flex-1 flex items-center justify-center gap-1.5 px-4 py-3 rounded-xl bg-primary text-white text-sm font-semibold hover:brightness-110 transition disabled:opacity-50"
              >
                {saving ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    Saving...
                  </>
                ) : (
                  <>
                    <Check className="w-4 h-4" />
                    Save Changes
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      )}

      <style>{`
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

        @keyframes deleteCollapse {
          from {
            opacity: 1;
            max-height: 500px;
            transform: scale(1);
          }
          to {
            opacity: 0;
            max-height: 0;
            transform: scale(0.95);
            margin: 0;
            padding: 0;
          }
        }

        .animate-scale-in {
          animation: scaleIn 0.3s cubic-bezier(0.23, 1, 0.32, 1) forwards;
        }

        .animate-delete {
          animation: deleteCollapse 0.3s cubic-bezier(0.23, 1, 0.32, 1) forwards;
          overflow: hidden;
        }
      `}</style>
    </div>
  );
}
