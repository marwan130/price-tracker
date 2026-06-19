import { useEffect, useState } from "react";
import { apiClient } from "@/lib/api/apiClient";
import { Activity, Loader2, CheckCircle, XCircle, Clock, RefreshCw } from "lucide-react";
import toast from "react-hot-toast";

interface ScrapeLog {
  logId: string;
  storeName: string;
  status: "success" | "failed" | "pending" | "running";
  productsScraped: number;
  errorMessage: string | null;
  startedAt: string;
  completedAt: string | null;
  duration: number | null;
}

type StatusFilter = "all" | "success" | "failed" | "pending" | "running";

export function AdminScrapeLogsPage() {
  const [logs, setLogs] = useState<ScrapeLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<StatusFilter>("all");

  useEffect(() => {
    let active = true;
    apiClient
      .get("/v1/admin/scrape-logs")
      .then((res) => {
        if (active && res.data?.success && Array.isArray(res.data.data)) {
          setLogs(res.data.data);
        }
      })
      .catch(() => {
        toast.error("Failed to load scrape logs");
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, []);

  const filteredLogs = logs.filter((log) => {
    if (filter === "all") return true;
    return log.status === filter;
  });

  const getStatusIcon = (status: ScrapeLog["status"]) => {
    switch (status) {
      case "success":
        return <CheckCircle className="w-4 h-4 text-cyan-400" />;
      case "failed":
        return <XCircle className="w-4 h-4 text-red-400" />;
      case "pending":
        return <Clock className="w-4 h-4 text-yellow-400" />;
      case "running":
        return <RefreshCw className="w-4 h-4 text-cyan-400 animate-spin" />;
    }
  };

  const getStatusBadge = (status: ScrapeLog["status"]) => {
    switch (status) {
      case "success":
        return "bg-cyan-500/10 text-cyan-400 border-cyan-500/20";
      case "failed":
        return "bg-red-500/10 text-red-400 border-red-500/20";
      case "pending":
        return "bg-yellow-500/10 text-yellow-400 border-yellow-500/20";
      case "running":
        return "bg-cyan-500/10 text-cyan-400 border-cyan-500/20 animate-pulse";
    }
  };

  const formatDuration = (ms: number | null) => {
    if (!ms) return "—";
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(1)}s`;
  };

  if (loading) {
    return (
      <div className="container mx-auto max-w-7xl px-4 py-8">
        <div className="flex flex-1 items-center justify-center min-h-[60vh]">
          <div className="flex flex-col items-center gap-3">
            <Loader2 className="w-10 h-10 text-cyan-500 animate-spin" />
            <p className="text-text-secondary text-sm font-medium">Loading scrape logs...</p>
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
            <Activity className="w-6 h-6 text-cyan-400" />
          </div>
          <h1 className="text-3xl font-display font-black tracking-tight text-white md:text-4xl">
            Scrape Logs
          </h1>
        </div>
        <p className="text-text-secondary text-sm ml-11">
          Monitor scraper execution and status
        </p>
      </div>

      {/* Filter Tabs */}
      <div className="flex gap-2 overflow-x-auto pb-2 reveal" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
        {(["all", "success", "failed", "pending", "running"] as StatusFilter[]).map((status) => (
          <button
            key={status}
            onClick={() => setFilter(status)}
            className={`px-4 py-2 rounded-full text-sm font-semibold whitespace-nowrap transition flex items-center gap-2 ${
              filter === status
                ? "bg-cyan-500 text-white shadow-lg shadow-cyan-500/15"
                : "bg-white/5 text-text-secondary hover:bg-white/10 hover:text-white"
            }`}
          >
            {filter === status && getStatusIcon(status as ScrapeLog["status"])}
            {status === "all" && "All"}
            {status === "success" && "Success"}
            {status === "failed" && "Failed"}
            {status === "pending" && "Pending"}
            {status === "running" && "Running"}
          </button>
        ))}
      </div>

      {/* Logs Table */}
      {filteredLogs.length === 0 ? (
        <div className="admin-card p-16 text-center reveal" style={{ "--reveal-delay": "200ms" } as React.CSSProperties}>
          <Activity className="w-16 h-16 mx-auto mb-4 text-text-muted opacity-50" />
          <h3 className="text-xl font-bold text-white mb-2">No logs found</h3>
          <p className="text-text-secondary">
            {filter === "all" ? "No scrape logs available" : `No ${filter} logs found`}
          </p>
        </div>
      ) : (
        <div className="admin-card p-6 reveal" style={{ "--reveal-delay": "200ms" } as React.CSSProperties}>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-cyan-500/20">
                  <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Store</th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Status</th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Products</th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Duration</th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Started</th>
                  <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Error</th>
                </tr>
              </thead>
              <tbody>
                {filteredLogs.map((log) => (
                  <tr key={log.logId} className="border-b border-cyan-500/10 hover:bg-cyan-500/5 transition">
                    <td className="py-3 px-4">
                      <span className="text-sm font-medium text-white">{log.storeName}</span>
                    </td>
                    <td className="py-3 px-4">
                      <div className={`inline-flex items-center gap-2 px-3 py-1 rounded-full text-xs font-semibold border ${getStatusBadge(log.status)}`}>
                        {getStatusIcon(log.status)}
                        <span className="capitalize">{log.status}</span>
                      </div>
                    </td>
                    <td className="py-3 px-4">
                      <span className="text-sm text-text-secondary font-mono">{log.productsScraped}</span>
                    </td>
                    <td className="py-3 px-4">
                      <span className="text-sm text-text-secondary font-mono">{formatDuration(log.duration)}</span>
                    </td>
                    <td className="py-3 px-4">
                      <span className="text-sm text-text-secondary">
                        {new Date(log.startedAt).toLocaleString()}
                      </span>
                    </td>
                    <td className="py-3 px-4">
                      {log.errorMessage ? (
                        <span className="text-xs text-red-400 max-w-xs truncate block" title={log.errorMessage}>
                          {log.errorMessage}
                        </span>
                      ) : (
                        <span className="text-sm text-text-muted">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

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
