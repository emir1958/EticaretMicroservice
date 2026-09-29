import React, { useEffect, useState } from "react";
import api from "../../services/api";
import { useCart } from "../../context/CartContext";
import { useAuth } from "../../context/AuthContext";
import {
  ShoppingCart,
  Package,
  LogIn,
  LogOut,
  User as UserIcon,
  ShieldCheck,
} from "lucide-react";
import { Link } from "react-router-dom";

export default function ProductList() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const { addToCart, cart, setIsCartOpen } = useCart();
  const { user, isAuthenticated, isAdmin, logout } = useAuth();

  useEffect(() => {
    // 🟢 Catalog ürünleri ile StockDb canlı stoklarını paralel çekip birleştiriyoruz
    Promise.all([
      api.get("/api/Products"),
      api.get("/api/stocks").catch((err) => {
        console.warn("Stok servisi yanıt vermedi:", err);
        return { data: [] };
      }),
    ])
      .then(([prodRes, stockRes]) => {
        const productList = Array.isArray(prodRes.data)
          ? prodRes.data
          : prodRes.data?.data || [];

        const stockList = Array.isArray(stockRes.data) ? stockRes.data : [];
        const stockMap = new Map(
          stockList.map((s) => [s.productId, s.availableStock]),
        );

        // Her ürüne güncel availableStock değerini ekliyoruz
        const mergedList = productList.map((p) => ({
          ...p,
          availableStock: stockMap.has(p.id) ? stockMap.get(p.id) : 0,
        }));

        setProducts(mergedList);
        setLoading(false);
      })
      .catch((err) => {
        console.error("Katalog çekme hatası:", err);
        setError(
          "Ürünler yüklenirken bir hata oluştu. Gateway veya Catalog servisi çalışıyor mu?",
        );
        setLoading(false);
      });
  }, []);

  const totalCartCount = cart.reduce((sum, i) => sum + i.quantity, 0);

  return (
    <div
      style={{
        maxWidth: "1000px",
        margin: "0 auto",
        padding: "24px",
        fontFamily: "sans-serif",
      }}
    >
      {/* Üst Bar */}
      <header
        style={{
          display: "flex",
          flexWrap: "wrap",
          justifyContent: "space-between",
          alignItems: "center",
          gap: "20px",
          marginBottom: "32px",
          paddingBottom: "16px",
          borderBottom: "1px solid #e5e7eb",
        }}
      >
        {isAdmin ? (
          <Link
            to="/admin/products"
            style={{
              display: "flex",
              alignItems: "center",
              gap: "6px",
              textDecoration: "none",
              color: "#1e293b",
              background: "#e2e8f0",
              border: "1px solid #cbd5e1",
              padding: "8px 14px",
              borderRadius: "8px",
              fontSize: "14px",
              fontWeight: "600",
              whiteSpace: "nowrap",
            }}
          >
            📦 Ürün Ekle (Admin)
          </Link>
        ) : (
          <div style={{ width: "120px" }} />
        )}

        <h2
          style={{
            margin: 0,
            textAlign: "center",
            flex: "1 1 auto",
            minWidth: "250px",
            color: "#0f172a",
          }}
        >
          🛍️ E-Ticaret Mağazası
        </h2>

        <div
          style={{
            display: "flex",
            flexWrap: "wrap",
            alignItems: "center",
            gap: "16px",
          }}
        >
          {isAuthenticated ? (
            <div style={{ display: "flex", alignItems: "center", gap: "10px" }}>
              {isAdmin ? (
                <Link
                  to="/admin/orders"
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "6px",
                    textDecoration: "none",
                    fontSize: "13px",
                    fontWeight: "bold",
                    color: "#fff",
                    background: "#0f172a",
                    border: "1px solid #334155",
                    padding: "6px 14px",
                    borderRadius: "20px",
                  }}
                >
                  <ShieldCheck size={16} color="#38bdf8" />
                  Yönetici Paneli
                </Link>
              ) : (
                <span
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "6px",
                    fontSize: "14px",
                    color: "#374151",
                    background: "#f3f4f6",
                    padding: "6px 12px",
                    borderRadius: "20px",
                  }}
                >
                  <UserIcon size={16} color="#6b7280" />
                  {user?.username || user?.email || "Hesabım"}
                </span>
              )}

              <button
                onClick={logout}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "4px",
                  border: "1px solid #d1d5db",
                  background: "#fff",
                  padding: "6px 12px",
                  borderRadius: "6px",
                  cursor: "pointer",
                  fontSize: "13px",
                  color: "#ef4444",
                }}
              >
                <LogOut size={14} /> Çıkış
              </button>
            </div>
          ) : (
            <Link
              to="/auth"
              style={{
                display: "flex",
                alignItems: "center",
                gap: "6px",
                textDecoration: "none",
                color: "#2563eb",
                fontWeight: "500",
                fontSize: "14px",
                background: "#eff6ff",
                padding: "8px 14px",
                borderRadius: "8px",
              }}
            >
              <LogIn size={16} /> Giriş Yap / Kaydol
            </Link>
          )}

          {!isAdmin && (
            <button
              onClick={() => setIsCartOpen(true)}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "8px",
                background: "#2563eb",
                color: "#fff",
                border: "none",
                padding: "10px 16px",
                borderRadius: "8px",
                fontWeight: "bold",
                fontSize: "14px",
                cursor: "pointer",
              }}
            >
              <ShoppingCart size={18} />
              <span>Sepet ({totalCartCount})</span>
            </button>
          )}
        </div>
      </header>

      {/* Durum Bildirimleri */}
      {loading && <p style={{ color: "#6b7280" }}>Ürünler yükleniyor...</p>}
      {error && (
        <div
          style={{
            background: "#fee2e2",
            color: "#b91c1c",
            padding: "12px",
            borderRadius: "8px",
            marginBottom: "20px",
          }}
        >
          {error}
        </div>
      )}

      {/* Ürün Listesi Grid */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))",
          gap: "20px",
        }}
      >
        {products.map((p) => {
          const isOutOfStock = p.availableStock <= 0;

          return (
            <div
              key={p.id}
              style={{
                border: "1px solid #e5e7eb",
                borderRadius: "12px",
                padding: "16px",
                display: "flex",
                flexDirection: "column",
                justifyContent: "space-between",
                background: "#fff",
                boxShadow: "0 1px 3px rgba(0,0,0,0.05)",
                position: "relative",
              }}
            >
              <Link
                to={`/product/${p.id}`}
                style={{ textDecoration: "none", color: "inherit" }}
              >
                <div
                  style={{
                    background: "#f9fafb",
                    height: "160px",
                    borderRadius: "8px",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    marginBottom: "12px",
                    overflow: "hidden",
                    position: "relative",
                  }}
                >
                  {/* 🟢 Tükendi Rozeti */}
                  {isOutOfStock && (
                    <div
                      style={{
                        position: "absolute",
                        top: "8px",
                        left: "8px",
                        background: "#dc2626",
                        color: "#fff",
                        fontSize: "11px",
                        fontWeight: "bold",
                        padding: "3px 8px",
                        borderRadius: "4px",
                        zIndex: 2,
                        letterSpacing: "0.5px",
                      }}
                    >
                      TÜKENDİ
                    </div>
                  )}

                  {p.imageUrl ? (
                    <img
                      src={p.imageUrl}
                      alt={p.name}
                      style={{
                        width: "100%",
                        height: "100%",
                        objectFit: "cover",
                        filter: isOutOfStock
                          ? "grayscale(80%) opacity(0.8)"
                          : "none",
                      }}
                    />
                  ) : (
                    <Package size={48} color="#9ca3af" />
                  )}
                </div>

                <h3
                  style={{
                    margin: "0 0 8px 0",
                    fontSize: "18px",
                    color: "#111827",
                  }}
                >
                  {p.name}
                </h3>
                <p
                  style={{
                    color: "#6b7280",
                    fontSize: "14px",
                    margin: "0 0 12px 0",
                    lineHeight: "1.4",
                  }}
                >
                  {p.description || "Açıklama bulunmuyor"}
                </p>
              </Link>

              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  marginTop: "12px",
                  paddingTop: "12px",
                  borderTop: "1px solid #f3f4f6",
                }}
              >
                <span
                  style={{
                    fontSize: "18px",
                    fontWeight: "bold",
                    color: isOutOfStock ? "#64748b" : "#111827",
                  }}
                >
                  {p.price} TL
                </span>

                {/* 🟢 Tükendi / Yönetici / Sepete Ekle Butonu */}
                <button
                  onClick={() => {
                    if (isAdmin || isOutOfStock) return;
                    addToCart({
                      id: p.id,
                      name: p.name,
                      price: p.price,
                      imageUrl: p.imageUrl,
                    });
                  }}
                  disabled={isAdmin || isOutOfStock}
                  style={{
                    background: isAdmin
                      ? "#94a3b8"
                      : isOutOfStock
                        ? "#cbd5e1"
                        : "#2563eb",
                    color: isOutOfStock ? "#64748b" : "#fff",
                    border: "none",
                    padding: "8px 14px",
                    borderRadius: "6px",
                    fontWeight: "bold",
                    cursor: isAdmin || isOutOfStock ? "not-allowed" : "pointer",
                    fontSize: "13px",
                    transition: "background 0.2s",
                  }}
                >
                  {isAdmin
                    ? "Yönetici Modu"
                    : isOutOfStock
                      ? "Tükendi"
                      : "Sepete Ekle"}
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
