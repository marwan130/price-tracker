import { useEffect, useState } from "react";
import { apiClient } from "@/lib/api/apiClient";
import { Store, Globe, ExternalLink, Loader2, Package } from "lucide-react";
import toast from "react-hot-toast";

interface StoreItem {
  storeId: string;
  name: string;
  websiteUrl: string | null;
  country: string | null;
  currency: string | null;
  listingCount: number;
  isActive: boolean;
  createdAt: string;
}

export function StoresDirectoryPage() {
  const [stores, setStores] = useState<StoreItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedStoreId, setExpandedStoreId] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    apiClient
      .get("/v1/stores")
      .then((res) => {
        if (active && res.data?.success && Array.isArray(res.data.data)) {
          setStores(res.data.data);
        }
      })
      .catch(() => {
        toast.error("Failed to load stores");
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, []);

  const getCountryFlag = (country: string | null) => {
    if (!country) return "🌐";
    const flagMap: Record<string, string> = {
      "United States": "🇺🇸",
      "United Kingdom": "🇬🇧",
      "Canada": "🇨🇦",
      "Australia": "🇦🇺",
      "Germany": "🇩🇪",
      "France": "🇫🇷",
      "Spain": "🇪🇸",
      "Italy": "🇮🇹",
      "Japan": "🇯🇵",
      "China": "🇨🇳",
      "India": "🇮🇳",
      "Brazil": "🇧🇷",
      "Mexico": "🇲🇽",
      "Netherlands": "🇳🇱",
      "Sweden": "🇸🇪",
      "Norway": "🇳🇴",
      "Denmark": "🇩🇰",
      "Finland": "🇫🇮",
      "Poland": "🇵🇱",
      "Belgium": "🇧🇪",
      "Austria": "🇦🇹",
      "Switzerland": "🇨🇭",
      "Ireland": "🇮🇪",
      "Portugal": "🇵🇹",
      "Greece": "🇬🇷",
      "Czech Republic": "🇨🇿",
      "Hungary": "🇭🇺",
      "Romania": "🇷🇴",
      "Bulgaria": "🇧🇬",
      "Croatia": "🇭🇷",
      "Slovenia": "🇸🇮",
      "Slovakia": "🇸🇰",
      "Estonia": "🇪🇪",
      "Latvia": "🇱🇻",
      "Lithuania": "🇱🇹",
      "Luxembourg": "🇱🇺",
      "Malta": "🇲🇹",
      "Cyprus": "🇨🇾",
      "South Korea": "🇰🇷",
      "Singapore": "🇸🇬",
      "Hong Kong": "🇭🇰",
      "Taiwan": "🇹🇼",
      "Thailand": "🇹🇭",
      "Vietnam": "🇻🇳",
      "Indonesia": "🇮🇩",
      "Malaysia": "🇲🇾",
      "Philippines": "🇵🇭",
      "New Zealand": "🇳🇿",
      "South Africa": "🇿🇦",
      "Argentina": "🇦🇷",
      "Chile": "🇨🇱",
      "Colombia": "🇨🇴",
      "Peru": "🇵🇪",
      "Venezuela": "🇻🇪",
      "Ecuador": "🇪🇨",
      "Uruguay": "🇺🇾",
      "Paraguay": "🇵🇾",
      "Bolivia": "🇧🇴",
      "Costa Rica": "🇨🇷",
      "Panama": "🇵🇦",
      "Guatemala": "🇬🇹",
      "El Salvador": "🇸🇻",
      "Honduras": "🇭🇳",
      "Nicaragua": "🇳🇮",
      "Cuba": "🇨🇺",
      "Dominican Republic": "🇩🇴",
      "Jamaica": "🇯🇲",
      "Haiti": "🇭🇹",
      "Trinidad and Tobago": "🇹🇹",
      "Bahamas": "🇧🇸",
      "Barbados": "🇧🇧",
      "Iceland": "🇮🇸",
      "Turkey": "🇹🇷",
      "Israel": "🇮🇱",
      "United Arab Emirates": "🇦🇪",
      "Saudi Arabia": "🇸🇦",
      "Qatar": "🇶🇦",
      "Kuwait": "🇰🇼",
      "Bahrain": "🇧🇭",
      "Oman": "🇴🇲",
      "Egypt": "🇪🇬",
      "Morocco": "🇲🇦",
      "Algeria": "🇩🇿",
      "Tunisia": "🇹🇳",
      "Libya": "🇱🇾",
      "Nigeria": "🇳🇬",
      "Kenya": "🇰🇪",
      "Ethiopia": "🇪🇹",
      "Ghana": "🇬🇭",
      "Russia": "🇷🇺",
      "Ukraine": "🇺🇦",
      "Belarus": "🇧🇾",
      "Kazakhstan": "🇰🇿",
      "Uzbekistan": "🇺🇿",
    };
    return flagMap[country] || "🌐";
  };

  if (loading) {
    return (
      <div className="container mx-auto max-w-7xl px-4 py-8">
        <div className="flex flex-1 items-center justify-center min-h-[60vh]">
          <div className="flex flex-col items-center gap-3">
            <Loader2 className="w-10 h-10 text-primary animate-spin" />
            <p className="text-text-secondary text-sm font-medium">Loading stores...</p>
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
          Stores Directory
        </h1>
        <p className="text-text-secondary text-sm mt-1">
          Browse all supported stores and their available product listings.
        </p>
      </div>

      {/* Stats */}
      <div className="reveal grid gap-4 md:grid-cols-3" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
        <div className="hp-glass-card p-6 flex items-center gap-4">
          <div className="p-3 rounded-xl bg-primary/20">
            <Store className="w-6 h-6 text-primary" />
          </div>
          <div>
            <p className="text-2xl font-bold text-white">{stores.length}</p>
            <p className="text-xs text-text-secondary">Total Stores</p>
          </div>
        </div>
        <div className="hp-glass-card p-6 flex items-center gap-4">
          <div className="p-3 rounded-xl bg-success/20">
            <Package className="w-6 h-6 text-success" />
          </div>
          <div>
            <p className="text-2xl font-bold text-white">
              {stores.reduce((sum, s) => sum + s.listingCount, 0)}
            </p>
            <p className="text-xs text-text-secondary">Total Listings</p>
          </div>
        </div>
        <div className="hp-glass-card p-6 flex items-center gap-4">
          <div className="p-3 rounded-xl bg-accent/20">
            <Globe className="w-6 h-6 text-accent" />
          </div>
          <div>
            <p className="text-2xl font-bold text-white">
              {new Set(stores.map(s => s.country)).size}
            </p>
            <p className="text-xs text-text-secondary">Countries</p>
          </div>
        </div>
      </div>

      {/* Stores Grid */}
      {stores.length === 0 ? (
        <div className="hp-glass-card p-16 text-center reveal" style={{ "--reveal-delay": "200ms" } as React.CSSProperties}>
          <Store className="w-16 h-16 mx-auto mb-4 text-text-muted opacity-50" />
          <h3 className="text-xl font-bold text-white mb-2">No stores found</h3>
          <p className="text-text-secondary">
            No stores are currently available in the directory.
          </p>
        </div>
      ) : (
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {stores.map((store, index) => {
            const isExpanded = expandedStoreId === store.storeId;
            
            return (
              <div
                key={store.storeId}
                className="reveal hp-glass-card relative overflow-hidden group"
                style={{ "--reveal-delay": `${(index + 1) * 50}ms` } as React.CSSProperties}
              >
                {/* Hover animated flag background */}
                <div className="absolute inset-0 opacity-0 group-hover:opacity-10 transition-opacity duration-500">
                  <div className="absolute top-0 right-0 text-9xl select-none pointer-events-none transform group-hover:scale-125 group-hover:rotate-12 transition-transform duration-500">
                    {getCountryFlag(store.country)}
                  </div>
                </div>

                {/* Content */}
                <div className="relative z-10">
                  <div 
                    className="p-6 cursor-pointer"
                    onClick={() => setExpandedStoreId(isExpanded ? null : store.storeId)}
                  >
                    <div className="flex items-start justify-between mb-4">
                      <div className="flex items-center gap-3">
                        <div className="w-12 h-12 rounded-xl bg-surface/50 flex items-center justify-center text-3xl group-hover:scale-110 group-hover:rotate-6 transition-transform duration-300">
                          {getCountryFlag(store.country)}
                        </div>
                        <div>
                          <h3 className="font-bold text-white">{store.name}</h3>
                          <p className="text-xs text-text-secondary">{store.country || "Global"}</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        {store.isActive && (
                          <div className="px-2 py-1 rounded-full bg-success/10 text-success text-[10px] font-black uppercase tracking-wider">
                            Active
                          </div>
                        )}
                        <div className={`transform transition-transform duration-300 ${isExpanded ? 'rotate-180' : ''}`}>
                          <svg className="w-4 h-4 text-text-secondary" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                          </svg>
                        </div>
                      </div>
                    </div>

                    <div className="space-y-3 mb-4">
                      <div className="flex items-center justify-between text-sm">
                        <span className="text-text-secondary">Listings</span>
                        <span className="font-mono font-bold text-white">{store.listingCount}</span>
                      </div>
                      <div className="flex items-center justify-between text-sm">
                        <span className="text-text-secondary">Currency</span>
                        <span className="font-mono font-bold text-white">{store.currency || "USD"}</span>
                      </div>
                    </div>

                    {store.websiteUrl && (
                      <a
                        href={store.websiteUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-flex items-center gap-2 text-sm text-primary hover:text-primary-light transition"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <ExternalLink className="w-4 h-4" />
                        Visit website
                      </a>
                    )}
                  </div>

                  {/* Expansion Panel */}
                  <div
                    className={`overflow-hidden transition-all duration-300 ease-in-out ${
                      isExpanded ? 'max-h-96 opacity-100' : 'max-h-0 opacity-0'
                    }`}
                  >
                    <div className="px-6 pb-6 pt-2 border-t border-white/10">
                      <h4 className="text-sm font-semibold text-white mb-3 flex items-center gap-2">
                        <Package className="w-4 h-4 text-primary" />
                        Active Listings Summary
                      </h4>
                      <div className="space-y-2">
                        <div className="flex items-center justify-between p-3 bg-surface/50 rounded-lg">
                          <span className="text-xs text-text-secondary">Total Products</span>
                          <span className="text-sm font-mono font-bold text-white">{store.listingCount}</span>
                        </div>
                        <div className="flex items-center justify-between p-3 bg-surface/50 rounded-lg">
                          <span className="text-xs text-text-secondary">Status</span>
                          <span className={`text-xs font-bold ${store.isActive ? 'text-success' : 'text-text-muted'}`}>
                            {store.isActive ? 'Actively Scraping' : 'Inactive'}
                          </span>
                        </div>
                        <div className="flex items-center justify-between p-3 bg-surface/50 rounded-lg">
                          <span className="text-xs text-text-secondary">Added On</span>
                          <span className="text-xs font-mono text-text-secondary">
                            {new Date(store.createdAt).toLocaleDateString()}
                          </span>
                        </div>
                      </div>
                      <button
                        className="mt-4 w-full py-2 rounded-lg bg-primary/20 text-primary text-sm font-semibold hover:bg-primary/30 transition"
                        onClick={() => window.location.href = `/products`}
                      >
                        Browse Products from {store.name}
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
