import React, { useEffect, useState } from "react";
import api from "../services/api";
import { useCart } from "../context/CartContext";
import { useAuth } from "../context/AuthContext";
import {
  ShoppingCart,
  Package,
  LogIn,
  LogOut,
  User as UserIcon,
} from "lucide-react";
import { Link } from "react-router-dom";

export default function ProductList() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const { addToCart, cart } = useCart();
  const { user, isAuthenticated, logout } = useAuth();

  useEffect(() => {
    api
      .get("/api/Products")
      .then((res) => {
        const responseData = Array.isArray(res.data)
          ? res.data
          : res.data?.data || [];

        setProducts(responseData);
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
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: "32px",
          paddingBottom: "16px",
          borderBottom: "1px solid #e5e7eb",
        }}
      >
        <Link
          to="/admin/products"
          style={{
            display: "flex",
            alignItems: "center",
            gap: "6px",
            textDecoration: "none",
            color: "#4b5563",
            border: "1px solid #d1d5db",
            padding: "8px 14px",
            borderRadius: "8px",
            fontSize: "14px",
            fontWeight: "500",
          }}
        >
          📦 Ürün Ekle (Admin)
        </Link>

        <h2 style={{ margin: 0 }}>🛍️ Mikroservis E-Ticaret Mağazası</h2>

        <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
          {isAuthenticated ? (
            <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
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
              }}
            >
              <LogIn size={16} /> Giriş Yap / Kaydol
            </Link>
          )}

          <Link
            to="/checkout"
            style={{
              display: "flex",
              alignItems: "center",
              gap: "8px",
              textDecoration: "none",
              background: "#2563eb",
              color: "#fff",
              padding: "8px 16px",
              borderRadius: "8px",
              fontWeight: "500",
            }}
          >
            <ShoppingCart size={18} />
            <span>Sepet ({totalCartCount})</span>
          </Link>
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
        // ... Diğer kodlar aynı kalacak
        {products.map((p) => (
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
            }}
          >
            {/* 🟢 Detaya Yönlendiren Link */}
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
                }}
              >
                {p.imageUrl ? (
                  <img
                    src={p.imageUrl}
                    alt={p.name}
                    style={{
                      width: "100%",
                      height: "100%",
                      objectFit: "cover",
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
                  color: "#111827",
                }}
              >
                {p.price} TL
              </span>
              <button
                onClick={() =>
                  addToCart({
                    id: p.id,
                    name: p.name,
                    price: p.price,
                    imageUrl: p.imageUrl,
                  })
                }
                style={{
                  background: "#2563eb",
                  color: "#fff",
                  border: "none",
                  padding: "8px 14px",
                  borderRadius: "6px",
                  fontWeight: "bold",
                  cursor: "pointer",
                }}
              >
                Sepete Ekle
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
