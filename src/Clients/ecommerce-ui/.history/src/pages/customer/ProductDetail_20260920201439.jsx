import React, { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import api from "../../services/api";
import { useCart } from "../../context/CartContext";
import { ArrowLeft, Package, Minus, Plus } from "lucide-react";

export default function ProductDetail() {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [stockCount, setStockCount] = useState(0);
  const [otherProducts, setOtherProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [quantity, setQuantity] = useState(1);
  const { addToCart } = useCart();

  useEffect(() => {
    // 1. Ürün, Stok ve Tüm Ürünleri paralel çekeriz
    const fetchData = async () => {
      setLoading(true);
      try {
        const [prodRes, stockRes, allProdsRes] = await Promise.all([
          api.get(`/api/Products/${id}`),
          api.get(`/api/stocks/${id}`).catch(() => ({ data: { count: 0 } })), // Stok yoksa 0 say
          api.get(`/api/Products`),
        ]);

        setProduct(prodRes.data?.data || prodRes.data);
        const available =
          stockRes.data?.availableStock ??
          stockRes.data?.data?.availableStock ??
          0;

        setStockCount(available);

        const allList = Array.isArray(allProdsRes.data)
          ? allProdsRes.data
          : allProdsRes.data?.data || [];
        // Mevcut ürünü listeden çıkar
        setOtherProducts(allList.filter((p) => p.id !== id).slice(0, 4));
      } catch (err) {
        console.error("Veriler alınamadı", err);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
    setQuantity(1); // Farklı bir ürüne geçince adeti sıfırla
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
    if (quantity < stockCount) setQuantity((q) => q + 1);
  };
  const handleSub = () => {
    if (quantity > 1) setQuantity((q) => q - 1);
  };

  return (
    <div
      style={{
        maxWidth: "1000px",
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

      <div
        style={{
          display: "flex",
          gap: "40px",
          flexWrap: "wrap",
          marginBottom: "60px",
        }}
      >
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
            minHeight: "400px",
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
          <h1
            style={{ fontSize: "32px", margin: "0 0 16px 0", color: "#0f172a" }}
          >
            {product.name}
          </h1>
          <p
            style={{
              color: "#475569",
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
              fontSize: "32px",
              fontWeight: "bold",
              color: "#0f766e",
              marginBottom: "12px",
            }}
          >
            {product.price} TL
          </div>
          <p
            style={{
              margin: "0 0 24px 0",
              color: stockCount > 0 ? "#10b981" : "#ef4444",
              fontWeight: "600",
              fontSize: "15px",
            }}
          >
            {stockCount > 0
              ? `✓ Stokta var (${stockCount} adet)`
              : "✗ Stokta yok"}
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
              style={{ fontSize: "14px", fontWeight: "600", color: "#334155" }}
            >
              Adet:
            </span>
            <div
              style={{
                display: "flex",
                alignItems: "center",
                border: "1px solid #cbd5e1",
                borderRadius: "8px",
                overflow: "hidden",
              }}
            >
              <button
                onClick={handleSub}
                disabled={quantity <= 1}
                style={{
                  padding: "12px 16px",
                  background: "#f8fafc",
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
                  fontSize: "16px",
                }}
              >
                {quantity}
              </div>
              <button
                onClick={handleAdd}
                disabled={quantity >= stockCount}
                style={{
                  padding: "12px 16px",
                  background: "#f8fafc",
                  border: "none",
                  cursor: quantity >= stockCount ? "not-allowed" : "pointer",
                }}
              >
                <Plus size={16} />
              </button>
            </div>
          </div>

          <button
            onClick={() => addToCart({ ...product, id: product.id }, quantity)}
            disabled={stockCount === 0}
            style={{
              background: stockCount === 0 ? "#94a3b8" : "#2563eb",
              color: "#fff",
              border: "none",
              padding: "16px",
              borderRadius: "8px",
              fontSize: "16px",
              fontWeight: "bold",
              cursor: stockCount === 0 ? "not-allowed" : "pointer",
              transition: "0.2s",
            }}
          >
            {stockCount === 0 ? "Tükendi" : "Sepete Ekle"}
          </button>
        </div>
      </div>

      {/* Diğer Ürünler Alanı */}
      {otherProducts.length > 0 && (
        <div style={{ borderTop: "1px solid #e2e8f0", paddingTop: "40px" }}>
          <h3
            style={{ margin: "0 0 20px 0", fontSize: "20px", color: "#0f172a" }}
          >
            Bunlar da İlginizi Çekebilir
          </h3>
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(200px, 1fr))",
              gap: "20px",
            }}
          >
            {otherProducts.map((op) => (
              <Link
                key={op.id}
                to={`/product/${op.id}`}
                style={{
                  textDecoration: "none",
                  color: "inherit",
                  border: "1px solid #e2e8f0",
                  borderRadius: "12px",
                  overflow: "hidden",
                  display: "flex",
                  flexDirection: "column",
                }}
              >
                <div
                  style={{
                    height: "140px",
                    background: "#f8fafc",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                  }}
                >
                  {op.imageUrl ? (
                    <img
                      src={op.imageUrl}
                      alt={op.name}
                      style={{
                        width: "100%",
                        height: "100%",
                        objectFit: "cover",
                      }}
                    />
                  ) : (
                    <Package color="#cbd5e1" size={40} />
                  )}
                </div>
                <div
                  style={{
                    padding: "16px",
                    display: "flex",
                    flexDirection: "column",
                    flex: 1,
                    justifyContent: "space-between",
                  }}
                >
                  <h4 style={{ margin: "0 0 8px 0", fontSize: "15px" }}>
                    {op.name}
                  </h4>
                  <strong style={{ color: "#0f766e" }}>{op.price} TL</strong>
                </div>
              </Link>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
