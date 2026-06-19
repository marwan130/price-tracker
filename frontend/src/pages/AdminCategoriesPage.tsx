import { useEffect, useState } from "react";
import { apiClient } from "@/lib/api/apiClient";
import { Edit2, Trash2, Loader2, Database, Save, X } from "lucide-react";
import toast from "react-hot-toast";

interface Category {
  categoryId: number;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
}

export function AdminCategoriesPage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editName, setEditName] = useState("");
  const [editDescription, setEditDescription] = useState("");
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  useEffect(() => {
    let active = true;
    apiClient
      .get("/v1/admin/categories")
      .then((res) => {
        if (active && res.data?.success && Array.isArray(res.data.data)) {
          setCategories(res.data.data);
        }
      })
      .catch(() => {
        toast.error("Failed to load categories");
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, []);

  const handleEdit = (category: Category) => {
    setEditingId(category.categoryId);
    setEditName(category.name);
    setEditDescription(category.description || "");
  };

  const handleSave = async () => {
    if (!editingId) return;
    
    try {
      setSaving(true);
      const res = await apiClient.put(`/v1/admin/categories/${editingId}`, {
        name: editName,
        description: editDescription,
      });

      if (res.data?.success) {
        setCategories(categories.map(c => 
          c.categoryId === editingId ? { ...c, name: editName, description: editDescription } : c
        ));
        toast.success("Category updated");
        setEditingId(null);
      }
    } catch (error) {
      toast.error("Failed to update category");
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setEditingId(null);
    setEditName("");
    setEditDescription("");
  };

  const handleDelete = async (categoryId: number) => {
    try {
      setDeletingId(categoryId);
      const res = await apiClient.delete(`/v1/admin/categories/${categoryId}`);

      if (res.data?.success) {
        setCategories(categories.filter(c => c.categoryId !== categoryId));
        toast.success("Category deleted");
      }
    } catch (error) {
      toast.error("Failed to delete category");
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
            <p className="text-text-secondary text-sm font-medium">Loading categories...</p>
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
            <Database className="w-6 h-6 text-cyan-400" />
          </div>
          <h1 className="text-3xl font-display font-black tracking-tight text-white md:text-4xl">
            Categories Management
          </h1>
        </div>
        <p className="text-text-secondary text-sm ml-11">
          Manage product categories
        </p>
      </div>

      {/* Categories Table */}
      <div className="admin-card p-6 reveal" style={{ "--reveal-delay": "100ms" } as React.CSSProperties}>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-cyan-500/20">
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">ID</th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Name</th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Description</th>
                <th className="text-left py-3 px-4 text-sm font-semibold text-cyan-400">Status</th>
                <th className="text-right py-3 px-4 text-sm font-semibold text-cyan-400">Actions</th>
              </tr>
            </thead>
            <tbody>
              {categories.map((category) => (
                <tr key={category.categoryId} className="border-b border-cyan-500/10 hover:bg-cyan-500/5 transition">
                  <td className="py-3 px-4 text-sm text-text-secondary font-mono">{category.categoryId}</td>
                  <td className="py-3 px-4">
                    {editingId === category.categoryId ? (
                      <input
                        type="text"
                        value={editName}
                        onChange={(e) => setEditName(e.target.value)}
                        className="w-full hp-input"
                      />
                    ) : (
                      <span className="text-sm font-medium text-white">{category.name}</span>
                    )}
                  </td>
                  <td className="py-3 px-4">
                    {editingId === category.categoryId ? (
                      <input
                        type="text"
                        value={editDescription}
                        onChange={(e) => setEditDescription(e.target.value)}
                        className="w-full hp-input"
                      />
                    ) : (
                      <span className="text-sm text-text-secondary">{category.description || "—"}</span>
                    )}
                  </td>
                  <td className="py-3 px-4">
                    <span className={`px-2 py-1 rounded-full text-[10px] font-black uppercase ${
                      category.isActive 
                        ? "bg-cyan-500/10 text-cyan-400 border-cyan-500/20" 
                        : "bg-red-500/10 text-red-400 border-red-500/20"
                    }`}>
                      {category.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td className="py-3 px-4 text-right">
                    {editingId === category.categoryId ? (
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
                          onClick={() => handleEdit(category)}
                          className="p-2 rounded-lg bg-white/10 text-text-secondary hover:bg-white/20 transition"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleDelete(category.categoryId)}
                          disabled={deletingId === category.categoryId}
                          className="p-2 rounded-lg bg-red-500/10 text-red-400 hover:bg-red-500/20 transition disabled:opacity-50"
                        >
                          {deletingId === category.categoryId ? <Loader2 className="w-4 h-4 animate-spin" /> : <Trash2 className="w-4 h-4" />}
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
