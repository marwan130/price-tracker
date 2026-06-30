import { useEffect, useState } from "react";
import { apiClient } from "@/lib/api/apiClient";
import { Edit2, Trash2, Loader2, Store, Save, X, Globe } from "lucide-react";
import toast from "react-hot-toast";

interface StoreItem {
  storeId: string;
  name: string;
  websiteUrl: string | null;
  country: string | null;
  currency: string | null;
  currencyCode?: string | null;
  isActive: boolean;
  scraperType: string;
  createdAt: string;
}

export function AdminStoresPage() {
  const [stores, setStores] = useState<StoreItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [editWebsite, setEditWebsite] = useState("");
  const [editCountry, setEditCountry] = useState("");
  const [editScraperType, setEditScraperType] = useState("Html");
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    apiClient
      .get("/v1/stores")
      .then((res) => {
        if (active && res.data?.success && Array.isArray(res.data.data)) {
          setStores(res.data.data.map((store: any) => ({
            storeId: store.storeId,
            name: store.name,
            websiteUrl: store.websiteUrl ?? store.baseUrl ?? null,
            country: store.country ?? null,
            currency: store.currency ?? store.currencyCode ?? null,
            currencyCode: store.currencyCode ?? store.currency ?? null,
            isActive: store.isActive,
            scraperType: String(store.scraperType ?? "Html"),
            createdAt: store.createdAt,
          })));
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

  const handleEdit = (store: StoreItem) => {
    setEditingId(store.storeId);
    setEditName(store.name);
    setEditWebsite(store.websiteUrl || "");
    setEditCountry(store.country || "");
    setEditScraperType(store.scraperType || "Html");
  };

  const handleSave = async () => {
    if (!editingId) return;
    
    try {
      setSaving(true);
      const current = stores.find(s => s.storeId === editingId);
      const res = await apiClient.put(`/v1/stores/${editingId}`, {
        name: editName,
        baseUrl: editWebsite,
        country: editCountry,
        currencyCode: current?.currencyCode ?? current?.currency ?? "USD",
        isActive: current?.isActive ?? true,
        scraperType: editScraperType,
      });

      if (res.data?.success) {
        setStores(stores.map(s => 
          s.storeId === editingId ? { ...s, name: editName, websiteUrl: editWebsite, country: editCountry, scraperType: editScraperType } : s
        ));
        toast.success("Store updated");
        setEditingId(null);
      }
    } catch (error) {
      toast.error("Failed to update store");
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setEditingId(null);
    setEditName("");
    setEditWebsite("");
    setEditCountry("");
    setEditScraperType("Html");
  };

  const handleDelete = async (storeId: string) => {
    try {
      setDeletingId(storeId);
      const res = await apiClient.delete(`/v1/stores/${storeId}`);

      if (res.data?.success) {
        setStores(stores.filter(s => s.storeId !== storeId));
        toast.success("Store deleted");
      }
    } catch (error) {
      toast.error("Failed to delete store");
    } finally {
      setDeletingId(null);
    }
  };

  if (loading) {
    return (
      <div className="container mx-auto max-w-7xl px-4 py-8">
        <div className="flex flex-1 items-center justify-center min-h-[60vh]">
          <div className="flex flex-col items-center gap-3">
            <Loader2 className="w-10 h-10 text-cyan-500 animate-spin" />
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
        <div className="flex items-center gap-3 mb-2">
          <div className="p-2 rounded-xl bg-cyan-500/20">
            <Store className="w-6 h-6 text-cyan-400" />
          </div>
          <h1 className="text-3xl font-display font-black tracking-tight text-white md:text-4xl">
            Stores Management
          </h1>
        </div>
        <p className="text-text-secondary text-sm ml-11">
          Manage supported stores
        </p>
      </div>

      {/* Stores Table */}
      <div className="admin-card p-6 reveal" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-cyan-500/20">
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">ID</th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Name</th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Website</th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Country</th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Scraper Type</th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Status</th>
                <th className="text-right py-3 px-4 text-sm font-semibold text-cyan-400">Actions</th>
              </tr>
            </thead>
            <tbody>
              {stores.map((store) => (
                <tr key={store.storeId} className="border-b border-cyan-500/10 hover:bg-cyan-500/5 transition">
                  <td className="py-3 px-4 text-sm text-text-secondary font-mono">{store.storeId.slice(0, 8)}...</td>
                  <td className="py-3 px-4">
                    {editingId === store.storeId ? (
                      <input
                        type="text"
                        value={editName}
                        onChange={(e) => setEditName(e.target.value)}
                        className="w-full hp-input"
                      />
                    ) : (
                      <span className="text-sm font-medium text-white">{store.name}</span>
                    )}
                  </td>
                  <td className="py-3 px-4">
                    {editingId === store.storeId ? (
                      <input
                        type="text"
                        value={editWebsite}
                        onChange={(e) => setEditWebsite(e.target.value)}
                        className="w-full hp-input"
                      />
                    ) : (
                      store.websiteUrl ? (
                        <a
                          href={store.websiteUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-sm text-cyan-400 hover:text-cyan-300 flex items-center gap-1"
                        >
                          <Globe className="w-3 h-3" />
                          Visit
                        </a>
                      ) : (
                        <span className="text-sm text-text-secondary">—</span>
                      )
                    )}
                  </td>
                  <td className="py-3 px-4">
                    {editingId === store.storeId ? (
                      <input
                        type="text"
                        value={editCountry}
                        onChange={(e) => setEditCountry(e.target.value)}
                        className="w-full hp-input"
                      />
                    ) : (
                      <span className="text-sm text-text-secondary">{store.country || "—"}</span>
                    )}
                  </td>
                  <td className="py-3 px-4">
                    {editingId === store.storeId ? (
                      <select
                        value={editScraperType}
                        onChange={(e) => setEditScraperType(e.target.value)}
                        className="w-full hp-input"
                      >
                        <option value="Html">Html</option>
                        <option value="Playwright">Playwright</option>
                        <option value="Api">Api</option>
                        <option value="Unsupported">Unsupported</option>
                      </select>
                    ) : (
                      <span className={`px-2 py-1 rounded-full text-[10px] font-black uppercase ${
                        store.scraperType === "Unsupported" 
                          ? "bg-red-500/10 text-red-400 border-red-500/20"
                          : store.scraperType === "Playwright"
                          ? "bg-purple-500/10 text-purple-400 border-purple-500/20"
                          : store.scraperType === "Api"
                          ? "bg-green-500/10 text-green-400 border-green-500/20"
                          : "bg-cyan-500/10 text-cyan-400 border-cyan-500/20"
                      }`}>
                        {store.scraperType}
                      </span>
                    )}
                  </td>
                  <td className="py-3 px-4">
                    <span className={`px-2 py-1 rounded-full text-[10px] font-black uppercase ${
                      store.isActive 
                        ? "bg-cyan-500/10 text-cyan-400 border-cyan-500/20" 
                        : "bg-red-500/10 text-red-400 border-red-500/20"
                    }`}>
                      {store.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td className="py-3 px-4 text-right">
                    {editingId === store.storeId ? (
                      <div className="flex justify-end gap-2">
                        <button
                          onClick={handleSave}
                          disabled={saving}
                          className="p-2 rounded-lg bg-cyan-500/20 text-cyan-400 hover:bg-cyan-500/30 transition disabled:opacity-50"
                        >
                          {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                        </button>
                        <button
                          onClick={handleCancel}
                          disabled={saving}
                          className="p-2 rounded-lg bg-white/10 text-text-secondary hover:bg-white/20 transition disabled:opacity-50"
                        >
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    ) : (
                      <div className="flex justify-end gap-2">
                        <button
                          onClick={() => handleEdit(store)}
                          className="p-2 rounded-lg bg-white/10 text-text-secondary hover:bg-white/20 transition"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleDelete(store.storeId)}
                          disabled={deletingId === store.storeId}
                          className="p-2 rounded-lg bg-red-500/10 text-red-400 hover:bg-red-500/20 transition disabled:opacity-50"
                        >
                          {deletingId === store.storeId ? <Loader2 className="w-4 h-4 animate-spin" /> : <Trash2 className="w-4 h-4" />}
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
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
