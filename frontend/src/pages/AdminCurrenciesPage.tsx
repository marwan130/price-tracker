import { useEffect, useState } from "react";
import { apiClient } from "@/lib/api/apiClient";
import { Edit2, Trash2, Loader2, DollarSign, Save, X } from "lucide-react";
import toast from "react-hot-toast";

interface Currency {
  currencyId: number;
  code: string;
  name: string;
  symbol: string;
  isActive: boolean;
  createdAt: string;
}

export function AdminCurrenciesPage() {
  const [currencies, setCurrencies] = useState<Currency[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editCode, setEditCode] = useState("");
  const [editName, setEditName] = useState("");
  const [editSymbol, setEditSymbol] = useState("");
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  useEffect(() => {
    let active = true;
    apiClient
      .get("/v1/admin/currencies")
      .then((res) => {
        if (active && res.data?.success && Array.isArray(res.data.data)) {
          setCurrencies(res.data.data);
        }
      })
      .catch(() => {
        toast.error("Failed to load currencies");
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, []);

  const handleEdit = (currency: Currency) => {
    setEditingId(currency.currencyId);
    setEditCode(currency.code);
    setEditName(currency.name);
    setEditSymbol(currency.symbol);
  };

  const handleSave = async () => {
    if (!editingId) return;
    
    try {
      setSaving(true);
      const res = await apiClient.put(`/v1/admin/currencies/${editingId}`, {
        code: editCode,
        name: editName,
        symbol: editSymbol,
      });

      if (res.data?.success) {
        setCurrencies(currencies.map(c => 
          c.currencyId === editingId ? { ...c, code: editCode, name: editName, symbol: editSymbol } : c
        ));
        toast.success("Currency updated");
        setEditingId(null);
      }
    } catch (error) {
      toast.error("Failed to update currency");
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setEditingId(null);
    setEditCode("");
    setEditName("");
    setEditSymbol("");
  };

  const handleDelete = async (currencyId: number) => {
    try {
      setDeletingId(currencyId);
      const res = await apiClient.delete(`/v1/admin/currencies/${currencyId}`);

      if (res.data?.success) {
        setCurrencies(currencies.filter(c => c.currencyId !== currencyId));
        toast.success("Currency deleted");
      }
    } catch (error) {
      toast.error("Failed to delete currency");
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
            <p className="text-text-secondary text-sm font-medium">Loading currencies...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto max-w-7xl px-4 py-8 space-y-8">
      {/* Header */}
      <div className="reveal reveal-up">
        <div className="flex items-center gap-3 mb-2">
          <div className="p-2 rounded-xl bg-cyan-500/20">
            <DollarSign className="w-6 h-6 text-cyan-400" />
          </div>
          <h1 className="text-3xl font-display font-black tracking-tight text-white md:text-4xl">
            Currencies Management
          </h1>
        </div>
        <p className="text-text-secondary text-sm ml-11">
          Manage supported currencies
        </p>
      </div>

      {/* Currencies Table */}
      <div className="reveal reveal-up admin-card p-6">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-cyan-500/20">
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">ID</th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Code</th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Name</th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Symbol</th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Status</th>
                <th className="text-right py-3 px-4 text-sm font-semibold text-cyan-400">Actions</th>
              </tr>
            </thead>
            <tbody>
              {currencies.map((currency) => (
                <tr key={currency.currencyId} className="border-b border-cyan-500/10 hover:bg-cyan-500/5 transition">
                  <td className="py-3 px-4 text-sm text-text-secondary font-mono">{currency.currencyId}</td>
                  <td className="py-3 px-4">
                    {editingId === currency.currencyId ? (
                      <input
                        type="text"
                        value={editCode}
                        onChange={(e) => setEditCode(e.target.value)}
                        className="w-full hp-input"
                      />
                    ) : (
                      <span className="text-sm font-medium text-white">{currency.code}</span>
                    )}
                  </td>
                  <td className="py-3 px-4">
                    {editingId === currency.currencyId ? (
                      <input
                        type="text"
                        value={editName}
                        onChange={(e) => setEditName(e.target.value)}
                        className="w-full hp-input"
                      />
                    ) : (
                      <span className="text-sm text-text-secondary">{currency.name}</span>
                    )}
                  </td>
                  <td className="py-3 px-4">
                    {editingId === currency.currencyId ? (
                      <input
                        type="text"
                        value={editSymbol}
                        onChange={(e) => setEditSymbol(e.target.value)}
                        className="w-full hp-input"
                      />
                    ) : (
                      <span className="text-sm text-text-secondary">{currency.symbol}</span>
                    )}
                  </td>
                  <td className="py-3 px-4">
                    <span className={`px-2 py-1 rounded-full text-[10px] font-black uppercase ${
                      currency.isActive 
                        ? "bg-cyan-500/10 text-cyan-400 border-cyan-500/20" 
                        : "bg-red-500/10 text-red-400 border-red-500/20"
                    }`}>
                      {currency.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td className="py-3 px-4 text-right">
                    {editingId === currency.currencyId ? (
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
                          onClick={() => handleEdit(currency)}
                          className="p-2 rounded-lg bg-white/10 text-text-secondary hover:bg-white/20 transition"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleDelete(currency.currencyId)}
                          disabled={deletingId === currency.currencyId}
                          className="p-2 rounded-lg bg-red-500/10 text-red-400 hover:bg-red-500/20 transition disabled:opacity-50"
                        >
                          {deletingId === currency.currencyId ? <Loader2 className="w-4 h-4 animate-spin" /> : <Trash2 className="w-4 h-4" />}
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
