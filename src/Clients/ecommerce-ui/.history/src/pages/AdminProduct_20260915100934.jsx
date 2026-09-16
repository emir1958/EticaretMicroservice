import React, { useState, useEffect } from "react";
import api from "../services/api";
import { Link } from "react-router-dom";
import { ArrowLeft, PlusCircle, Package } from "lucide-react";

export default function AdminProduct() {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [price, setPrice] = useState("");
  const [stock, setStock] = useState("");
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState(null);
  const [error, setError] = useState(null);

  const fetchProducts = async () => {
    try {
      const res = await api.get("/api/Products");
      const list = Array.isArray(res.data) ? res.data : res.data?.data || [];
      setProducts(list);
    } catch (err) {
      console.error("Ürünler getirilemedi:", err);
    }
  };

  useEffect(() => {
    fetchProducts();
  }, []);

  const handleCreateProduct = async (e) => {
    e.preventDefault();
    setLoading(true);
    setMessage(null);
    setError(null);

    const payload = {
      name: name.trim(),
      description: description.trim(),
      price: parseFloat(price),
      stock: parseInt(stock, 10) || 100,
    };

    try {
      // YARP Gateway -> Catalog.Api /api/Products
      await api.post("/api/Products", payload);
      setMessage("Ürün başarıyla eklendi!");
      setName("");
      setDescription("");
      setPrice("");
      setStock("");
      fetchProducts();
    } catch (err) {
      console.error("Ürün ekleme hatası:", err);
      setError(
        err.response?.data?.message ||
          err.response?.data?.detail ||
          "Ürün eklenirken bir hata oluştu.",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        maxWidth: "800px",
        margin: "40px auto",
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

      <h2>📦 Admin Panel: Ürün Yönetimi</h2>

      {/* Ürün Ekleme Formu */}
      <div
        style={{
          background: "#f8fafc",
          border: "1px solid #e2e8f0",
          borderRadius: "12px",
          padding: "20px",
          marginBottom: "32px",
        }}
      >
        <h3
          style={{
            margin: "0 0 16px 0",
            display: "flex",
            alignItems: "center",
            gap: "8px",
          }}
        >
          <PlusCircle size={20} color="#2563eb" /> Yeni Ürün Ekle
        </h3>

        {message && (
          <div
            style={{
              background: "#dcfce7",
              color: "#15803d",
              padding: "10px",
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
              padding: "10px",
              borderRadius: "8px",
              marginBottom: "16px",
            }}
          >
            {error}
          </div>
        )}

        <form
          onSubmit={handleCreateProduct}
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gap: "16px",
          }}
        >
          <div style={{ gridColumn: "span 2" }}>
            <label
              style={{
                display: "block",
                fontSize: "13px",
                fontWeight: "500",
                marginBottom: "6px",
              }}
            >
              Ürün Adı
            </label>
            <input
              type="text"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Örn: Kablosuz Mouse"
              style={{
                width: "100%",
                padding: "10px",
                borderRadius: "6px",
                border: "1px solid #cbd5e1",
                boxSizing: "border-box",
              }}
            />
          </div>

          <div style={{ gridColumn: "span 2" }}>
            <label
              style={{
                display: "block",
                fontSize: "13px",
                fontWeight: "500",
                marginBottom: "6px",
              }}
            >
              Açıklama
            </label>
            <textarea
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Ürün özellikleri..."
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
                marginBottom: "6px",
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
              placeholder="1299.90"
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
                marginBottom: "6px",
              }}
            >
              Stok Adedi
            </label>
            <input
              type="number"
              required
              value={stock}
              onChange={(e) => setStock(e.target.value)}
              placeholder="50"
              style={{
                width: "100%",
                padding: "10px",
                borderRadius: "6px",
                border: "1px solid #cbd5e1",
                boxSizing: "border-box",
              }}
            />
          </div>

          <div style={{ gridColumn: "span 2" }}>
            <button
              type="submit"
              disabled={loading}
              style={{
                background: "#2563eb",
                color: "#fff",
                border: "none",
                padding: "12px 20px",
                borderRadius: "8px",
                fontWeight: "bold",
                cursor: loading ? "not-allowed" : "pointer",
              }}
            >
              {loading ? "Ekleniyor..." : "Ürünü Kaydet"}
            </button>
          </div>
        </form>
      </div>

      {/* Eklenmiş Ürünlerin Listesi */}
      <h3>Mevcut Katalog Ürünleri ({products.length})</h3>
      {products.length === 0 ? (
        <p style={{ color: "#64748b" }}>
          Henüz sistemde kayıtlı ürün bulunmuyor.
        </p>
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: "10px" }}>
          {products.map((p) => (
            <div
              key={p.id}
              style={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                padding: "12px 16px",
                background: "#fff",
                border: "1px solid #e2e8f0",
                borderRadius: "8px",
              }}
            >
              <div
                style={{ display: "flex", alignItems: "center", gap: "12px" }}
              >
                <Package size={24} color="#64748b" />
                <div>
                  <strong>{p.name}</strong>
                  <p style={{ margin: 0, fontSize: "13px", color: "#64748b" }}>
                    {p.description || "Açıklama yok"}
                  </p>
                </div>
              </div>
              <strong style={{ color: "#10b981" }}>{p.price} TL</strong>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
