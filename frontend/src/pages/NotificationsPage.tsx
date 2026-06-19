import { useEffect, useState } from "react";
import { apiClient } from "@/lib/api/apiClient";
import { Bell, Check, CheckCheck, Trash2, Loader2, AlertCircle, TrendingDown, ShoppingBag, Target } from "lucide-react";
import toast from "react-hot-toast";

interface NotificationItem {
  notificationId: string;
  userId: string;
  type: "price_drop" | "target_reached" | "new_product" | "system";
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
  metadata?: Record<string, unknown>;
}

type NotificationType = "all" | "unread" | "price_drop" | "target_reached";

export function NotificationsPage() {
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<NotificationType>("all");
  const [markingAsRead, setMarkingAsRead] = useState<Set<string>>(new Set());
  const [deleting, setDeleting] = useState<Set<string>>(new Set());

  useEffect(() => {
    let active = true;
    apiClient
      .get("/v1/notifications")
      .then((res) => {
        if (active && res.data?.success && Array.isArray(res.data.data)) {
          setNotifications(res.data.data);
        }
      })
      .catch(() => {
        toast.error("Failed to load notifications");
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, []);

  const filteredNotifications = notifications.filter((notif) => {
    if (filter === "all") return true;
    if (filter === "unread") return !notif.isRead;
    return notif.type === filter;
  });

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  const handleMarkAsRead = async (notificationId: string) => {
    try {
      setMarkingAsRead(prev => new Set(prev).add(notificationId));
      const res = await apiClient.put(`/v1/notifications/${notificationId}/read`);

      if (res.data?.success) {
        setNotifications(notifications.map(n => 
          n.notificationId === notificationId ? { ...n, isRead: true } : n
        ));
      }
    } catch (error) {
      toast.error("Failed to mark as read");
    } finally {
      setMarkingAsRead(prev => {
        const newSet = new Set(prev);
        newSet.delete(notificationId);
        return newSet;
      });
    }
  };

  const handleMarkAllAsRead = async () => {
    const unreadIds = notifications.filter(n => !n.isRead).map(n => n.notificationId);
    if (unreadIds.length === 0) return;

    try {
      setMarkingAsRead(new Set(unreadIds));
      const res = await apiClient.put("/v1/notifications/read-all");

      if (res.data?.success) {
        setNotifications(notifications.map(n => ({ ...n, isRead: true })));
        toast.success("All notifications marked as read");
      }
    } catch (error) {
      toast.error("Failed to mark all as read");
    } finally {
      setMarkingAsRead(new Set());
    }
  };

  const handleDelete = async (notificationId: string) => {
    try {
      setDeleting(prev => new Set(prev).add(notificationId));
      const res = await apiClient.delete(`/v1/notifications/${notificationId}`);

      if (res.data?.success) {
        setNotifications(notifications.filter(n => n.notificationId !== notificationId));
        toast.success("Notification deleted");
      }
    } catch (error) {
      toast.error("Failed to delete notification");
    } finally {
      setDeleting(prev => {
        const newSet = new Set(prev);
        newSet.delete(notificationId);
        return newSet;
      });
    }
  };

  const getNotificationIcon = (type: NotificationItem["type"]) => {
    switch (type) {
      case "price_drop":
        return <TrendingDown className="w-5 h-5 text-accent" />;
      case "target_reached":
        return <Target className="w-5 h-5 text-success" />;
      case "new_product":
        return <ShoppingBag className="w-5 h-5 text-primary" />;
      default:
        return <AlertCircle className="w-5 h-5 text-warning" />;
    }
  };

  const getNotificationTypeColor = (type: NotificationItem["type"]) => {
    switch (type) {
      case "price_drop":
        return "bg-accent/10 border-accent/20";
      case "target_reached":
        return "bg-success/10 border-success/20";
      case "new_product":
        return "bg-primary/10 border-primary/20";
      default:
        return "bg-warning/10 border-warning/20";
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "Just now";
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString();
  };

  if (loading) {
    return (
      <div className="container mx-auto max-w-4xl px-4 py-8">
        <div className="flex flex-1 items-center justify-center min-h-[60vh]">
          <div className="flex flex-col items-center gap-3">
            <Loader2 className="w-10 h-10 text-primary animate-spin" />
            <p className="text-text-secondary text-sm font-medium">Loading notifications...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto max-w-4xl px-4 py-8 space-y-8">
      {/* Header */}
      <div className="reveal flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-display font-black tracking-tight text-white md:text-4xl">
            Notifications
          </h1>
          <p className="text-text-secondary text-sm mt-1">
            {unreadCount > 0 ? `${unreadCount} unread notification${unreadCount === 1 ? '' : 's'}` : "All caught up"}
          </p>
        </div>
        {unreadCount > 0 && (
          <button
            onClick={handleMarkAllAsRead}
            disabled={markingAsRead.size > 0}
            className="btn-ieee flex items-center gap-2 bg-primary/20 px-4 py-2 rounded-full text-primary text-sm font-semibold hover:bg-primary/30 transition disabled:opacity-50"
          >
            <CheckCheck className="w-4 h-4" />
            Mark all as read
          </button>
        )}
      </div>

      {/* Filter Tabs */}
      <div className="reveal flex gap-2 overflow-x-auto pb-2" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
        {(["all", "unread", "price_drop", "target_reached"] as NotificationType[]).map((filterType) => (
          <button
            key={filterType}
            onClick={() => setFilter(filterType)}
            className={`px-4 py-2 rounded-full text-sm font-semibold whitespace-nowrap transition ${
              filter === filterType
                ? "bg-primary text-white shadow-lg shadow-primary/15"
                : "bg-white/5 text-text-secondary hover:bg-white/10 hover:text-white"
            }`}
          >
            {filterType === "all" && "All"}
            {filterType === "unread" && "Unread"}
            {filterType === "price_drop" && "Price Drops"}
            {filterType === "target_reached" && "Target Reached"}
          </button>
        ))}
      </div>

      {/* Notifications List */}
      {filteredNotifications.length === 0 ? (
        <div className="hp-glass-card p-16 text-center reveal" style={{ "--reveal-delay": "200ms" } as React.CSSProperties}>
          <Bell className="w-16 h-16 mx-auto mb-4 text-text-muted opacity-50" />
          <h3 className="text-xl font-bold text-white mb-2">No notifications</h3>
          <p className="text-text-secondary">
            {filter === "unread" ? "No unread notifications" : "No notifications found"}
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {filteredNotifications.map((notification, index) => (
            <div
              key={notification.notificationId}
              className={`reveal hp-glass-card p-5 relative overflow-hidden transition-all ${
                !notification.isRead ? "border-primary/30 bg-primary/5" : "border-white/5"
              }`}
              style={{ "--reveal-delay": `${(index + 1) * 50}ms` } as React.CSSProperties}
            >
              {/* Unread pulse indicator */}
              {!notification.isRead && (
                <div className="absolute top-5 left-5">
                  <div className="w-2 h-2 rounded-full bg-primary animate-pulse" />
                </div>
              )}

              <div className="flex items-start gap-4 pl-4">
                {/* Icon */}
                <div className={`p-3 rounded-xl ${getNotificationTypeColor(notification.type)} flex-shrink-0`}>
                  {getNotificationIcon(notification.type)}
                </div>

                {/* Content */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-2 mb-1">
                    <h3 className={`font-semibold ${!notification.isRead ? "text-white" : "text-text-secondary"}`}>
                      {notification.title}
                    </h3>
                    <span className="text-xs text-text-muted whitespace-nowrap">
                      {formatDate(notification.createdAt)}
                    </span>
                  </div>
                  <p className="text-sm text-text-secondary line-clamp-2">
                    {notification.message}
                  </p>
                </div>

                {/* Actions */}
                <div className="flex items-center gap-2 flex-shrink-0">
                  {!notification.isRead && (
                    <button
                      onClick={() => handleMarkAsRead(notification.notificationId)}
                      disabled={markingAsRead.has(notification.notificationId)}
                      className="p-2 rounded-lg bg-white/5 text-text-secondary hover:bg-white/10 hover:text-white transition disabled:opacity-50"
                      title="Mark as read"
                    >
                      {markingAsRead.has(notification.notificationId) ? (
                        <Loader2 className="w-4 h-4 animate-spin" />
                      ) : (
                        <Check className="w-4 h-4" />
                      )}
                    </button>
                  )}
                  <button
                    onClick={() => handleDelete(notification.notificationId)}
                    disabled={deleting.has(notification.notificationId)}
                    className="p-2 rounded-lg bg-accent-secondary/10 text-accent-secondary hover:bg-accent-secondary/20 transition disabled:opacity-50"
                    title="Delete"
                  >
                    {deleting.has(notification.notificationId) ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <Trash2 className="w-4 h-4" />
                    )}
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
