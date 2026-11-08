import React, { useState, useEffect } from "react";
import api from "../../services/api";
import AdminLayout from "../../components/AdminLayout";
import { Link } from "react-router-dom";
import {
  ArrowLeft,
  PlusCircle,
  Edit,
  Trash2,
  X,
  Image as ImageIcon,
} from "lucide-react";

export default function AdminProduct() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState(null);
  const [error, setError] = useState(null);

  // Form State
  const [editingId, setEditingId] = useState(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [price, setPrice] = useState("");
  const [stock, setStock] = useState("");
  const [imageUrl, setImageUrl] = useState("");

  // 🟢 1. Ürünleri Catalog'dan, Gerçek Stokları Stock.Api'den Çekip Birleştiriyoruz
  const fetchProducts = async () => {
    try {
      const [prodRes, stockRes] = await Promise.all([
        api.get("/api/Products"),
        api.get("/api/stocks").catch(() => ({ data: [] })),
      ]);

      const productList = Array.isArray(prodRes.data)
        ? prodRes.data
        : prodRes.data?.data || [];

      const stockList = Array.isArray(stockRes.data) ? stockRes.data : [];
      const stockMap = new Map(
        stockList.map((s) => [s.productId, s.availableStock]),
      );

      // Catalog ürünlerine StockDb'deki gerçek AvailableStock değerini eşliyoruz
      const mergedProducts = productList.map((p) => ({
        ...p,
        availableStock: stockMap.has(p.id) ? stockMap.get(p.id) : 0,
      }));

      setProducts(mergedProducts);
    } catch (err) {
      console.error("Ürünler getirilemedi:", err);
    }
  };

  useEffect(() => {
    fetchProducts();
  }, []);

  const resetForm = () => {
    setEditingId(null);
    setName("");
    setDescription("");
    setPrice("");
    setStock("");
    setImageUrl("");
  };

  // 🟢 2. Düzenleye Basıldığında StockDb'den Anlık Sorgu Atıyoruz
  const handleEditClick = async (p) => {
    setEditingId(p.id);
    setName(p.name);
    setDescription(p.description || "");
    setPrice(p.price);
    setImageUrl(p.imageUrl || "");
    setStock(p.availableStock ?? 0); // Varsayılan birleştirilmiş stok

    window.scrollTo({ top: 0, behavior: "smooth" });

    // En güncel StockDb değerini tekil sorguyla garantiye al
    try {
      const res = await api.get(`/api/stocks/${p.id}`);
      if (res.data?.availableStock !== undefined) {
        setStock(res.data.availableStock);
      }
    } catch (err) {
      console.warn("Tekil stok çekilemedi, listedeki değer kullanılıyor:", err);
    }
  };

  // 🟢 3. Kaydet'e Basıldığında Catalog ve Stock.Api Ayrı Ayrı Güncellenir
  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setMessage(null);
    setError(null);

    const stockNumber = parseInt(stock, 10) || 0;

    try {
      if (editingId) {
        // A. Catalog Servisi: Ürün Metaverilerini Güncelle
        await api.put(`/api/Products/${editingId}`, {
          name: name.trim(),
          description: description.trim(),
          price: parseFloat(price),
          imageUrl: imageUrl.trim(),
        });

        // B. Stock Servisi: Gerçek Stoğu StockDb Üzerinde Güncelle
        await api.put(`/api/stocks/${editingId}`, {
          availableStock: stockNumber,
        });

        setMessage("Ürün bilgileri ve StockDb stoğu başarıyla güncellendi.");
      } else {
        // Yeni Ürün Ekleme (Catalog başlangıç stoğunu event ile Stock'a fırlatır)
        await api.post("/api/Products", {
          name: name.trim(),
          description: description.trim(),
          price: parseFloat(price),
          imageUrl: imageUrl.trim(),
          initialStock: stockNumber,
        });
        setMessage("Yeni ürün başarıyla eklendi.");
      }

      resetForm();
      fetchProducts();
    } catch (err) {
      console.error("İşlem hatası:", err);
      setError(err.response?.data?.message || "İşlem gerçekleştirilemedi.");
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm("Bu ürünü silmek istediğinize emin misiniz?")) return;

    try {
      await api.delete(`/api/Products/${id}`);
      setMessage("Ürün silindi.");
      fetchProducts();
    } catch (err) {
      console.error("Silme hatası:", err);
      setError(
        err.response?.data?.message || "Ürün silinirken bir hata oluştu.",
      );
    }
  };

  return (
    <AdminLayout>
      <div
        style={{
          maxWidth: "960px",
          margin: "30px auto",
          padding: "24px",
          fontFamily: "sans-serif",
        }}
      >
        <Link
          to="/"
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: "6px",
            textDecoration: "none",
            color: "#2563eb",
            marginBottom: "20px",
          }}
        >
          <ArrowLeft size={18} /> Mağazaya Dön
        </Link>

        <h2 style={{ margin: "0 0 20px 0" }}>⚙️ Ürün Yönetim Paneli</h2>

        {message && (
          <div
            style={{
              background: "#dcfce7",
              color: "#15803d",
              padding: "12px",
              borderRadius: "8px",
              marginBottom: "16px",
            }}
          >
            {message}
          </div>
        )}
        {error && (
          <div
            style={{
              background: "#fee2e2",
              color: "#b91c1c",
              padding: "12px",
              borderRadius: "8px",
              marginBottom: "16px",
            }}
          >
            {error}
          </div>
        )}

        {/* Form Alanı */}
        <div
          style={{
            background: "#f8fafc",
            border: "1px solid #e2e8f0",
            borderRadius: "12px",
            padding: "20px",
            marginBottom: "32px",
          }}
        >
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              marginBottom: "16px",
            }}
          >
            <h3
              style={{
                margin: 0,
                display: "flex",
                alignItems: "center",
                gap: "8px",
                fontSize: "18px",
              }}
            >
              {editingId ? (
                <Edit size={20} color="#2563eb" />
              ) : (
                <PlusCircle size={20} color="#10b981" />
              )}
              {editingId ? "Ürünü Düzenle" : "Yeni Ürün Ekle"}
            </h3>
            {editingId && (
              <button
                onClick={resetForm}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "4px",
                  border: "none",
                  background: "transparent",
                  color: "#64748b",
                  cursor: "pointer",
                  fontSize: "13px",
                }}
              >
                <X size={16} /> Düzenlemeyi İptal Et
              </button>
            )}
          </div>

          <form
            onSubmit={handleSubmit}
            style={{
              display: "grid",
              gridTemplateColumns: "2fr 1fr 1fr",
              gap: "16px",
            }}
          >
            <div>
              <label
                style={{
                  display: "block",
                  fontSize: "13px",
                  fontWeight: "500",
                  marginBottom: "4px",
                }}
              >
                Ürün Adı
              </label>
              <input
                type="text"
                required
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Örn: Logi MX Master 3S"
                style={{
                  width: "100%",
                  padding: "10px",
                  borderRadius: "6px",
                  border: "1px solid #cbd5e1",
                  boxSizing: "border-box",
                }}
              />
            </div>

            <div>
              <label
                style={{
                  display: "block",
                  fontSize: "13px",
                  fontWeight: "500",
                  marginBottom: "4px",
                }}
              >
                Fiyat (TL)
              </label>
              <input
                type="number"
                step="0.01"
                required
                value={price}
                onChange={(e) => setPrice(e.target.value)}
                placeholder="3499.00"
                style={{
                  width: "100%",
                  padding: "10px",
                  borderRadius: "6px",
                  border: "1px solid #cbd5e1",
                  boxSizing: "border-box",
                }}
              />
            </div>

            <div>
              <label
                style={{
                  display: "block",
                  fontSize: "13px",
                  fontWeight: "500",
                  marginBottom: "4px",
                }}
              >
                {editingId ? "Güncel Stok (StockDb)" : "Başlangıç Stoğu"}
              </label>
              <input
                type="number"
                required
                value={stock}
                onChange={(e) => setStock(e.target.value)}
                placeholder="100"
                style={{
                  width: "100%",
                  padding: "10px",
                  borderRadius: "6px",
                  border: "1px solid #cbd5e1",
                  boxSizing: "border-box",
                }}
              />
            </div>

            <div style={{ gridColumn: "span 3" }}>
              <label
                style={{
                  display: "block",
                  fontSize: "13px",
                  fontWeight: "500",
                  marginBottom: "4px",
                }}
              >
                Görsel URL
              </label>
              <input
                type="url"
                value={imageUrl}
                onChange={(e) => setImageUrl(e.target.value)}
                placeholder="https://images.unsplash.com/photo-..."
                style={{
                  width: "100%",
                  padding: "10px",
                  borderRadius: "6px",
                  border: "1px solid #cbd5e1",
                  boxSizing: "border-box",
                }}
              />
            </div>

            <div style={{ gridColumn: "span 3" }}>
              <label
                style={{
                  display: "block",
                  fontSize: "13px",
                  fontWeight: "500",
                  marginBottom: "4px",
                }}
              >
                Açıklama
              </label>
              <input
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Kısa ürün açıklaması..."
                style={{
                  width: "100%",
                  padding: "10px",
                  borderRadius: "6px",
                  border: "1px solid #cbd5e1",
                  boxSizing: "border-box",
                }}
              />
            </div>

            <div style={{ gridColumn: "span 3" }}>
              <button
                type="submit"
                disabled={loading}
                style={{
                  background: editingId ? "#2563eb" : "#10b981",
                  color: "#fff",
                  border: "none",
                  padding: "12px 24px",
                  borderRadius: "8px",
                  fontWeight: "bold",
                  cursor: loading ? "not-allowed" : "pointer",
                }}
              >
                {loading
                  ? "İşleniyor..."
                  : editingId
                    ? "Değişiklikleri Kaydet"
                    : "Ürünü Kaydet"}
              </button>
            </div>
          </form>
        </div>

        {/* Ürün Listesi */}
        <h3 style={{ marginBottom: "12px" }}>
          Katalogdaki Ürünler ({products.length})
        </h3>
        <table
          style={{
            width: "100%",
            borderCollapse: "collapse",
            background: "#fff",
            border: "1px solid #e2e8f0",
            borderRadius: "8px",
            overflow: "hidden",
          }}
        >
          <thead>
            <tr
              style={{
                background: "#f1f5f9",
                textAlign: "left",
                fontSize: "13px",
                color: "#475569",
              }}
            >
              <th style={{ padding: "12px" }}>Görsel</th>
              <th style={{ padding: "12px" }}>Ürün Adı</th>
              <th style={{ padding: "12px" }}>Açıklama</th>
              <th style={{ padding: "12px" }}>Fiyat</th>
              {/* 🟢 Stok Kolonu Eklendi */}
              <th style={{ padding: "12px" }}>Mevcut Stok</th>
              <th style={{ padding: "12px", textAlign: "right" }}>İşlemler</th>
            </tr>
          </thead>
          <tbody>
            {products.length === 0 ? (
              <tr>
                <td
                  colSpan={6}
                  style={{
                    padding: "24px",
                    textAlign: "center",
                    color: "#64748b",
                  }}
                >
                  Kayıtlı ürün bulunamadı.
                </td>
              </tr>
            ) : (
              products.map((p) => (
                <tr
                  key={p.id}
                  style={{
                    borderBottom: "1px solid #f1f5f9",
                    fontSize: "14px",
                  }}
                >
                  <td style={{ padding: "12px", width: "50px" }}>
                    {p.imageUrl ? (
                      <img
                        src={p.imageUrl}
                        alt={p.name}
                        style={{
                          width: "44px",
                          height: "44px",
                          objectFit: "cover",
                          borderRadius: "6px",
                        }}
                      />
                    ) : (
                      <div
                        style={{
                          width: "44px",
                          height: "44px",
                          background: "#f1f5f9",
                          borderRadius: "6px",
                          display: "flex",
                          alignItems: "center",
                          justifyContent: "center",
                        }}
                      >
                        <ImageIcon size={18} color="#94a3b8" />
                      </div>
                    )}
                  </td>
                  <td style={{ padding: "12px", fontWeight: "600" }}>
                    {p.name}
                  </td>
                  <td style={{ padding: "12px", color: "#64748b" }}>
                    {p.description || "-"}
                  </td>
                  <td
                    style={{
                      padding: "12px",
                      fontWeight: "bold",
                      color: "#0f766e",
                    }}
                  >
                    {p.price} TL
                  </td>
                  {/* 🟢 StockDb'den Gelen Canlı Stok Değeri */}
                  <td style={{ padding: "12px" }}>
                    <span
                      style={{
                        background:
                          p.availableStock > 0 ? "#dcfce7" : "#fee2e2",
                        color: p.availableStock > 0 ? "#15803d" : "#b91c1c",
                        padding: "4px 8px",
                        borderRadius: "4px",
                        fontWeight: "bold",
                        fontSize: "12px",
                      }}
                    >
                      {p.availableStock} Adet
                    </span>
                  </td>
                  <td style={{ padding: "12px", textAlign: "right" }}>
                    <button
                      onClick={() => handleEditClick(p)}
                      style={{
                        background: "#e0f2fe",
                        color: "#0369a1",
                        border: "none",
                        padding: "6px 10px",
                        borderRadius: "6px",
                        cursor: "pointer",
                        marginRight: "8px",
                      }}
                    >
                      <Edit size={14} /> Düzenle
                    </button>
                    <button
                      onClick={() => handleDelete(p.id)}
                      style={{
                        background: "#fee2e2",
                        color: "#b91c1c",
                        border: "none",
                        padding: "6px 10px",
                        borderRadius: "6px",
                        cursor: "pointer",
                      }}
                    >
                      <Trash2 size={14} /> Sil
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </AdminLayout>
  );
}
