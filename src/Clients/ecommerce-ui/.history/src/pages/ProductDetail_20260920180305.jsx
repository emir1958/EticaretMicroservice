import React, { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import api from "../services/api";
import { useCart } from "../context/CartContext";
import { ArrowLeft, Package, Minus, Plus } from "lucide-react";

export default function ProductDetail() {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);
  const [quantity, setQuantity] = useState(1);
  const { addToCart } = useCart();

  useEffect(() => {
    api
      .get(`/api/Products/${id}`)
      .then((res) => {
        setProduct(res.data?.data || res.data);
        setLoading(false);
      })
      .catch((err) => {
        console.error("Ürün detayı alınamadı", err);
        setLoading(false);
      });
  }, [id]);

  if (loading)
    return (
      <div style={{ padding: "40px", textAlign: "center" }}>Yükleniyor...</div>
    );
  if (!product)
    return (
      <div style={{ padding: "40px", textAlign: "center" }}>
        Ürün bulunamadı.
      </div>
    );

  const handleAdd = () => {
    if (quantity < (product.stock || 99)) setQuantity((q) => q + 1);
  };
  const handleSub = () => {
    if (quantity > 1) setQuantity((q) => q - 1);
  };

  return (
    <div
      style={{
        maxWidth: "900px",
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
          marginBottom: "24px",
        }}
      >
        <ArrowLeft size={18} /> Mağazaya Dön
      </Link>

      <div style={{ display: "flex", gap: "40px", flexWrap: "wrap" }}>
        {/* Görsel */}
        <div
          style={{
            flex: "1 1 400px",
            background: "#f9fafb",
            borderRadius: "16px",
            overflow: "hidden",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            minHeight: "300px",
          }}
        >
          {product.imageUrl ? (
            <img
              src={product.imageUrl}
              alt={product.name}
              style={{ width: "100%", height: "100%", objectFit: "cover" }}
            />
          ) : (
            <Package size={80} color="#9ca3af" />
          )}
        </div>

        {/* Detaylar */}
        <div
          style={{
            flex: "1 1 300px",
            display: "flex",
            flexDirection: "column",
            justifyContent: "center",
          }}
        >
          <h1 style={{ fontSize: "32px", margin: "0 0 16px 0" }}>
            {product.name}
          </h1>
          <p
            style={{
              color: "#6b7280",
              fontSize: "16px",
              lineHeight: "1.6",
              marginBottom: "24px",
            }}
          >
            {product.description ||
              "Bu ürün için henüz bir açıklama girilmemiştir."}
          </p>
          <div
            style={{
              fontSize: "28px",
              fontWeight: "bold",
              color: "#111827",
              marginBottom: "8px",
            }}
          >
            {product.price} TL
          </div>
          <p
            style={{
              margin: "0 0 24px 0",
              color: product.stock > 0 ? "#10b981" : "#ef4444",
              fontWeight: "500",
            }}
          >
            {product.stock > 0
              ? `Stokta var (${product.stock} adet)`
              : "Stokta yok"}
          </p>

          {/* Miktar Seçici */}
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: "16px",
              marginBottom: "24px",
            }}
          >
            <span
              style={{ fontSize: "14px", fontWeight: "500", color: "#374151" }}
            >
              Adet:
            </span>
            <div
              style={{
                display: "flex",
                alignItems: "center",
                border: "1px solid #d1d5db",
                borderRadius: "8px",
                overflow: "hidden",
              }}
            >
              <button
                onClick={handleSub}
                disabled={quantity <= 1}
                style={{
                  padding: "10px",
                  background: "#f3f4f6",
                  border: "none",
                  cursor: quantity <= 1 ? "not-allowed" : "pointer",
                }}
              >
                <Minus size={16} />
              </button>
              <div
                style={{
                  width: "40px",
                  textAlign: "center",
                  fontWeight: "bold",
                }}
              >
                {quantity}
              </div>
              <button
                onClick={handleAdd}
                disabled={quantity >= (product.stock || 99)}
                style={{
                  padding: "10px",
                  background: "#f3f4f6",
                  border: "none",
                  cursor:
                    quantity >= (product.stock || 99)
                      ? "not-allowed"
                      : "pointer",
                }}
              >
                <Plus size={16} />
              </button>
            </div>
          </div>

          <button
            onClick={() => addToCart({ ...product, id: product.id }, quantity)}
            disabled={product.stock === 0}
            style={{
              background: product.stock === 0 ? "#9ca3af" : "#2563eb",
              color: "#fff",
              border: "none",
              padding: "16px",
              borderRadius: "8px",
              fontSize: "16px",
              fontWeight: "bold",
              cursor: product.stock === 0 ? "not-allowed" : "pointer",
            }}
          >
            {product.stock === 0 ? "Tükendi" : "Sepete Ekle"}
          </button>
        </div>
      </div>
    </div>
  );
}
