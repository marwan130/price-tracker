import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiClient } from "@/lib/api/apiClient";
import { 
  Shield, 
  Database, 
  Activity, 
  Settings, 
  Users, 
  Store, 
  DollarSign, 
  TrendingUp,
  Loader2,
  CheckCircle,
  XCircle,
  Clock
} from "lucide-react";
import toast from "react-hot-toast";

interface DashboardStats {
  totalProducts: number;
  totalListings: number;
  totalStores: number;
  totalUsers: number;
  activeScrapers: number;
  failedScrapers: number;
  pendingScrapers: number;
}

interface RecentScrapeLog {
  logId: string;
  storeName: string;
  status: "success" | "failed" | "pending";
  productsScraped: number;
  errorMessage: string | null;
  executedAt: string;
}

interface ScrapeLogApi {
  logId?: string | number;
  store?: { name?: string | null } | null;
  storeName?: string | null;
  status?: string | number | null;
  itemsScraped?: number | null;
  errorMessage?: string | null;
  finishedAt?: string | null;
  startedAt?: string | null;
}

const normalizeStatus = (status: string): RecentScrapeLog["status"] => {
  const normalized = status.toLowerCase();
  if (normalized === "failed" || normalized === "1") return "failed";
  if (normalized === "partial" || normalized === "pending" || normalized === "running" || normalized === "2") return "pending";
  return "success";
};

export function AdminDashboardPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [recentLogs, setRecentLogs] = useState<RecentScrapeLog[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const [productsRes, storesRes, logsRes] = await Promise.all([
          apiClient.get("/v1/products", { params: { page: 0, size: 1 } }),
          apiClient.get("/v1/stores"),
          apiClient.get("/v1/scrape-logs", { params: { page: 0, size: 5 } })
        ]);

        const logs = logsRes.data?.success && Array.isArray(logsRes.data.data?.content)
          ? logsRes.data.data.content
          : [];

        if (productsRes.data?.success && storesRes.data?.success) {
          const failed = logs.filter((log: ScrapeLogApi) => normalizeStatus(String(log.status)) === "failed").length;
          const pending = logs.filter((log: ScrapeLogApi) => normalizeStatus(String(log.status)) === "pending").length;
          setStats({
            totalProducts: productsRes.data.data?.totalElements ?? 0,
            totalListings: 0,
            totalStores: Array.isArray(storesRes.data.data) ? storesRes.data.data.length : 0,
            totalUsers: 0,
            activeScrapers: Math.max(logs.length - failed - pending, 0),
            failedScrapers: failed,
            pendingScrapers: pending,
          });
        }

        setRecentLogs(logs.map((log: ScrapeLogApi) => ({
          logId: String(log.logId),
          storeName: log.store?.name ?? log.storeName ?? "Unknown store",
          status: normalizeStatus(String(log.status)),
          productsScraped: log.itemsScraped ?? 0,
          errorMessage: log.errorMessage ?? null,
          executedAt: log.finishedAt ?? log.startedAt,
        })));
      } catch {
        toast.error("Failed to load admin dashboard data");
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  const getStatusIcon = (status: RecentScrapeLog["status"]) => {
    switch (status) {
      case "success":
        return <CheckCircle className="w-4 h-4 text-cyan-400" />;
      case "failed":
        return <XCircle className="w-4 h-4 text-red-400" />;
      case "pending":
        return <Clock className="w-4 h-4 text-yellow-400" />;
    }
  };

  const getStatusBadge = (status: RecentScrapeLog["status"]) => {
    switch (status) {
      case "success":
        return "bg-cyan-500/10 text-cyan-400 border-cyan-500/20";
      case "failed":
        return "bg-red-500/10 text-red-400 border-red-500/20";
      case "pending":
        return "bg-yellow-500/10 text-yellow-400 border-yellow-500/20";
    }
  };

  if (loading) {
    return (
      <div className="container mx-auto max-w-7xl px-4 py-8">
        <div className="flex flex-1 items-center justify-center min-h-[60vh]">
          <div className="flex flex-col items-center gap-3">
            <Loader2 className="w-10 h-10 text-cyan-500 animate-spin" />
            <p className="text-text-secondary text-sm font-medium">Loading admin dashboard...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto max-w-7xl px-4 py-8 space-y-8">
      {/* Header */}
      <div className="reveal">
        <div className="flex items-center gap-3 mb-2">
          <div className="p-2 rounded-xl bg-cyan-500/20">
            <Shield className="w-6 h-6 text-cyan-400" />
          </div>
          <h1 className="text-3xl font-display font-black tracking-tight text-white md:text-4xl">
            Admin Dashboard
          </h1>
        </div>
        <p className="text-text-secondary text-sm ml-11">
          System overview and management controls
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <div className="admin-card p-6 relative overflow-hidden reveal" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
          <div className="absolute top-0 right-0 p-4 opacity-10">
            <Database className="w-16 h-16 text-cyan-400" />
          </div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 rounded-lg bg-cyan-500/20">
              <Database className="w-5 h-5 text-cyan-400" />
            </div>
            <span className="text-text-secondary text-sm">Total Products</span>
          </div>
          <p className="text-3xl font-bold text-white">{stats?.totalProducts || 0}</p>
        </div>

        <div className="admin-card p-6 relative overflow-hidden reveal" style={{ "--reveal-delay": "200ms" } as React.CSSProperties}>
          <div className="absolute top-0 right-0 p-4 opacity-10">
            <Store className="w-16 h-16 text-cyan-400" />
          </div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 rounded-lg bg-cyan-500/20">
              <Store className="w-5 h-5 text-cyan-400" />
            </div>
            <span className="text-text-secondary text-sm">Total Listings</span>
          </div>
          <p className="text-3xl font-bold text-white">{stats?.totalListings || 0}</p>
        </div>

        <div className="admin-card p-6 relative overflow-hidden reveal" style={{ "--reveal-delay": "300ms" } as React.CSSProperties}>
          <div className="absolute top-0 right-0 p-4 opacity-10">
            <Users className="w-16 h-16 text-cyan-400" />
          </div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 rounded-lg bg-cyan-500/20">
              <Users className="w-5 h-5 text-cyan-400" />
            </div>
            <span className="text-text-secondary text-sm">Total Users</span>
          </div>
          <p className="text-3xl font-bold text-white">{stats?.totalUsers || 0}</p>
        </div>

        <div className="admin-card p-6 relative overflow-hidden reveal" style={{ "--reveal-delay": "400ms" } as React.CSSProperties}>
          <div className="absolute top-0 right-0 p-4 opacity-10">
            <DollarSign className="w-16 h-16 text-cyan-400" />
          </div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 rounded-lg bg-cyan-500/20">
              <DollarSign className="w-5 h-5 text-cyan-400" />
            </div>
            <span className="text-text-secondary text-sm">Total Stores</span>
          </div>
          <p className="text-3xl font-bold text-white">{stats?.totalStores || 0}</p>
        </div>
      </div>

      {/* Scraper Status */}
      <div className="admin-card p-6 reveal" style={{ "--reveal-delay": "500ms" } as React.CSSProperties}>
        <h2 className="text-xl font-display font-bold text-white mb-6 flex items-center gap-2">
          <Activity className="w-5 h-5 text-cyan-400" />
          Scraper Status
        </h2>
        <div className="grid gap-4 md:grid-cols-3">
          <div className="p-4 bg-cyan-500/10 rounded-xl border border-cyan-500/20">
            <div className="flex items-center justify-between mb-2">
              <span className="text-text-secondary text-sm">Active</span>
              <CheckCircle className="w-4 h-4 text-cyan-400" />
            </div>
            <p className="text-2xl font-bold text-cyan-400">{stats?.activeScrapers || 0}</p>
          </div>
          <div className="p-4 bg-yellow-500/10 rounded-xl border border-yellow-500/20">
            <div className="flex items-center justify-between mb-2">
              <span className="text-text-secondary text-sm">Pending</span>
              <Clock className="w-4 h-4 text-yellow-400" />
            </div>
            <p className="text-2xl font-bold text-yellow-400">{stats?.pendingScrapers || 0}</p>
          </div>
          <div className="p-4 bg-red-500/10 rounded-xl border border-red-500/20">
            <div className="flex items-center justify-between mb-2">
              <span className="text-text-secondary text-sm">Failed</span>
              <XCircle className="w-4 h-4 text-red-400" />
            </div>
            <p className="text-2xl font-bold text-red-400">{stats?.failedScrapers || 0}</p>
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="admin-card p-6 reveal" style={{ "--reveal-delay": "600ms" } as React.CSSProperties}>
        <h2 className="text-xl font-display font-bold text-white mb-6 flex items-center gap-2">
          <Settings className="w-5 h-5 text-cyan-400" />
          Quick Actions
        </h2>
        <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-4">
          <Link
            to="/admin/categories"
            className="p-4 bg-cyan-500/10 rounded-xl border border-cyan-500/20 hover:bg-cyan-500/20 transition flex items-center gap-3"
          >
            <Database className="w-5 h-5 text-cyan-400" />
            <span className="font-semibold text-white">Manage Categories</span>
          </Link>
          <Link
            to="/admin/currencies"
            className="p-4 bg-cyan-500/10 rounded-xl border border-cyan-500/20 hover:bg-cyan-500/20 transition flex items-center gap-3"
          >
            <DollarSign className="w-5 h-5 text-cyan-400" />
            <span className="font-semibold text-white">Manage Currencies</span>
          </Link>
          <Link
            to="/admin/stores"
            className="p-4 bg-cyan-500/10 rounded-xl border border-cyan-500/20 hover:bg-cyan-500/20 transition flex items-center gap-3"
          >
            <Store className="w-5 h-5 text-cyan-400" />
            <span className="font-semibold text-white">Manage Stores</span>
          </Link>
          <Link
            to="/admin/scrape-logs"
            className="p-4 bg-cyan-500/10 rounded-xl border border-cyan-500/20 hover:bg-cyan-500/20 transition flex items-center gap-3"
          >
            <Activity className="w-5 h-5 text-cyan-400" />
            <span className="font-semibold text-white">View Scrape Logs</span>
          </Link>
        </div>
      </div>

      {/* Recent Scrape Logs */}
      <div className="admin-card p-6 reveal" style={{ "--reveal-delay": "700ms" } as React.CSSProperties}>
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-display font-bold text-white flex items-center gap-2">
            <TrendingUp className="w-5 h-5 text-cyan-400" />
            Recent Scrape Logs
          </h2>
          <Link
            to="/admin/scrape-logs"
            className="text-sm text-cyan-400 hover:text-cyan-300 transition"
          >
            View all →
          </Link>
        </div>
        {recentLogs.length === 0 ? (
          <div className="text-center py-8 text-text-secondary">
            <Activity className="w-12 h-12 mx-auto mb-3 opacity-50" />
            <p>No recent scrape logs</p>
          </div>
        ) : (
          <div className="space-y-3">
            {recentLogs.map((log) => (
              <div
                key={log.logId}
                className="flex items-center justify-between p-4 bg-surface/50 rounded-xl"
              >
                <div className="flex items-center gap-4">
                  <div className={`p-2 rounded-lg ${getStatusBadge(log.status)} border`}>
                    {getStatusIcon(log.status)}
                  </div>
                  <div>
                    <p className="font-semibold text-white">{log.storeName}</p>
                    <p className="text-xs text-text-secondary">
                      {log.productsScraped} products scraped
                    </p>
                  </div>
                </div>
                <div className="text-right">
                  <p className="text-xs text-text-secondary">
                    {new Date(log.executedAt).toLocaleString()}
                  </p>
                  {log.errorMessage && (
                    <p className="text-xs text-red-400 mt-1">{log.errorMessage}</p>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <style>{`
        .admin-card {
          background: rgba(8, 145, 178, 0.05);
          backdrop-filter: blur(12px);
          -webkit-backdrop-filter: blur(12px);
          border: 1px solid rgba(6, 182, 212, 0.2);
          box-shadow: 0 8px 32px -4px rgba(0, 0, 0, 0.5), 0 1px 3px rgba(6, 182, 212, 0.1);
          border-radius: 1.5rem;
        }
      `}</style>
    </div>
  );
}
