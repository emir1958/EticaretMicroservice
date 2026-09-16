import React, { useEffect, useState } from "react";
import api from "../services/api";
import { useCart } from "../context/CartContext";
import { ShoppingCart, Package } from "lucide-react";
import { Link } from "react-router-dom";

export default function ProductList() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const { addToCart, cart } = useCart();

  useEffect(() => {
    api
      .get("/api/Products")
      .then((res) => {
        setProducts(res.data);
        setLoading(false);
      })
      .catch((err) => {
        console.error("Katalog çekme hatası:", err);
        setError("Ürünler yüklenirken bir hata oluştu.");
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
      <header
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: "32px",
        }}
      >
        <h2>🛍️ Mikroservis E-Ticaret Mağazası</h2>
        <Link
          to="/checkout"
          style={{
            display: "flex",
            alignItems: "center",
            gap: "8px",
            textDecoration: "none",
            background: "#2563eb",
            color: "#fff",
            padding: "10px 16px",
            borderRadius: "8px",
          }}
        >
          <ShoppingCart size={20} />
          <span>Sepet ({totalCartCount})</span>
        </Link>
      </header>

      {loading && <p>Ürünler yükleniyor...</p>}
      {error && <p style={{ color: "red" }}>{error}</p>}

      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))",
          gap: "20px",
        }}
      >
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
            }}
          >
            <div>
              <div
                style={{
                  background: "#f3f4f6",
                  height: "140px",
                  borderRadius: "8px",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  marginBottom: "12px",
                }}
              >
                <Package size={48} color="#9ca3af" />
              </div>
              <h3 style={{ margin: "0 0 8px 0", fontSize: "18px" }}>
                {p.name}
              </h3>
              <p
                style={{
                  color: "#4b5563",
                  fontSize: "14px",
                  margin: "0 0 12px 0",
                }}
              >
                {p.description || "Açıklama bulunmuyor"}
              </p>
            </div>
            <div
              style={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                marginTop: "12px",
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
                onClick={() => addToCart(p)}
                style={{
                  background: "#10b981",
                  color: "#fff",
                  border: "none",
                  padding: "8px 14px",
                  borderRadius: "6px",
                  cursor: "pointer",
                  fontWeight: "500",
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
