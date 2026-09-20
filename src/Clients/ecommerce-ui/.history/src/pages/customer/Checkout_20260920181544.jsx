import React, { useState } from "react";
import { useCart } from "../../context/CartContext";
import { useAuth } from "../../context/AuthContext";
import { useOrderHub } from "../../hooks/useOrderHub";
import api from "../../services/api";
import { Link } from "react-router-dom";
import {
  ArrowLeft,
  Trash2,
  CheckCircle,
  XCircle,
  Clock,
  ShoppingBag,
} from "lucide-react";

export default function Checkout() {
  const { cart, totalAmount, removeFromCart, updateQuantity, clearCart } =
    useCart();
  const { user } = useAuth();
  const { orderStatus } = useOrderHub();

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [createdOrderId, setCreatedOrderId] = useState(null);
  const [errorMessage, setErrorMessage] = useState(null);

  const handleCreateOrder = async () => {
    if (cart.length === 0) return;
    setIsSubmitting(true);
    setErrorMessage(null);

    const resolvedBuyerId =
      user?.id ||
      user?.userId ||
      user?.sub ||
      user?.nameid ||
      "a2f49b67-7dba-4e09-bc50-2e740470ff29";

    const payload = {
      buyerId: resolvedBuyerId,
      address: {
        city: "İstanbul",
        district: "Kadıköy",
        street: "Moda Caddesi",
        zipCode: "34710",
        line: "No:12 Daire:4",
      },
      payment: {
        paymentToken: "tok_visa_sample_mock",
      },
      // 🟢 productName ve price alanları eklendi
      orderItems: cart.map((item) => ({
        productId: item.id,
        productName: item.name,
        price: item.price,
        quantity: item.quantity,
      })),
    };

    try {
      const response = await api.post("/api/orders", payload);
      setCreatedOrderId(response.data?.orderId || response.data);
      clearCart();
    } catch (err) {
      console.error("Sipariş oluşturma hatası:", err);
      const backendErrors = err.response?.data?.errors;
      let detailMsg = err.response?.data?.detail || err.response?.data?.message;

      if (backendErrors) {
        detailMsg = Object.values(backendErrors).flat().join(" | ");
      }

      setErrorMessage(
        detailMsg || "Sipariş oluşturulurken bir hata meydana geldi.",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div
      style={{
        maxWidth: "750px",
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

      <h2>Sipariş ve Ödeme Ekranı</h2>

      {/* SİPARİŞ OLUŞTURULDUYSA CANLI SAGA TAKİBİ */}
      {createdOrderId ? (
        <div
          style={{
            background: "#f8fafc",
            border: "1px solid #cbd5e1",
            borderRadius: "12px",
            padding: "24px",
            marginTop: "20px",
          }}
        >
          <h3 style={{ margin: "0 0 12px 0" }}>
            Siparişiniz Alındı! (No: #{createdOrderId})
          </h3>
          <p style={{ color: "#64748b", fontSize: "14px" }}>
            Event-Driven Saga devrede. Stok rezervasyonu ve ödeme adımları
            işleniyor...
          </p>

          <div
            style={{
              marginTop: "20px",
              padding: "16px",
              background: "#fff",
              borderRadius: "8px",
              border: "1px solid #e2e8f0",
            }}
          >
            <h4>Canlı Sipariş Durumu (SignalR):</h4>
            {orderStatus && orderStatus.orderId === createdOrderId ? (
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "12px",
                  marginTop: "12px",
                }}
              >
                {orderStatus.status === "Completed" ||
                orderStatus.status === "Tamamlandı" ? (
                  <>
                    <CheckCircle color="#10b981" size={28} />
                    <div>
                      <strong style={{ color: "#10b981" }}>
                        Sipariş Başarıyla Tamamlandı!
                      </strong>
                      <p
                        style={{
                          margin: 0,
                          fontSize: "14px",
                          color: "#64748b",
                        }}
                      >
                        Stok rezerve edildi ve ödeme başarıyla onaylandı.
                      </p>
                    </div>
                  </>
                ) : (
                  <>
                    <XCircle color="#ef4444" size={28} />
                    <div>
                      <strong style={{ color: "#ef4444" }}>
                        Sipariş İptal Edildi
                      </strong>
                      <p
                        style={{
                          margin: 0,
                          fontSize: "14px",
                          color: "#64748b",
                        }}
                      >
                        {orderStatus.message ||
                          "Telafi işlemi (compensation) yapıldı."}
                      </p>
                    </div>
                  </>
                )}
              </div>
            ) : (
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "12px",
                  marginTop: "12px",
                  color: "#f59e0b",
                }}
              >
                <Clock size={24} />
                <span>Beklemede: Servisler arası mesajlaşma sürüyor...</span>
              </div>
            )}
          </div>
        </div>
      ) : (
        /* SEPET LİSTESİ */
        <div>
          {cart.length === 0 ? (
            <div
              style={{
                textAlign: "center",
                padding: "48px 20px",
                background: "#f8fafc",
                borderRadius: "12px",
                border: "1px dashed #cbd5e1",
                marginTop: "20px",
              }}
            >
              <ShoppingBag
                size={48}
                color="#94a3b8"
                style={{ marginBottom: "12px" }}
              />
              <p style={{ color: "#64748b", margin: 0, fontSize: "16px" }}>
                Sepetinizde ürün bulunmuyor.
              </p>
            </div>
          ) : (
            <div>
              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  margin: "16px 0",
                }}
              >
                <span style={{ fontSize: "14px", color: "#64748b" }}>
                  Sepetteki Ürünler ({cart.length})
                </span>
                <button
                  onClick={clearCart}
                  style={{
                    background: "transparent",
                    border: "none",
                    color: "#ef4444",
                    cursor: "pointer",
                    fontSize: "13px",
                    textDecoration: "underline",
                  }}
                >
                  Sepeti Temizle
                </button>
              </div>

              <div
                style={{
                  display: "flex",
                  flexDirection: "column",
                  gap: "12px",
                }}
              >
                {cart.map((item) => (
                  <div
                    key={item.id}
                    style={{
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "space-between",
                      padding: "12px 16px",
                      border: "1px solid #e2e8f0",
                      borderRadius: "8px",
                      background: "#fff",
                    }}
                  >
                    <div
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "12px",
                      }}
                    >
                      {item.imageUrl ? (
                        <img
                          src={item.imageUrl}
                          alt={item.name}
                          style={{
                            width: "48px",
                            height: "48px",
                            objectFit: "cover",
                            borderRadius: "6px",
                          }}
                        />
                      ) : (
                        <div
                          style={{
                            width: "48px",
                            height: "48px",
                            background: "#f1f5f9",
                            borderRadius: "6px",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            fontSize: "11px",
                            color: "#94a3b8",
                          }}
                        >
                          Resim Yok
                        </div>
                      )}
                      <div>
                        <strong style={{ display: "block", color: "#1e293b" }}>
                          {item.name}
                        </strong>
                        <span style={{ fontSize: "13px", color: "#64748b" }}>
                          Birim Fiyat: {item.price} TL
                        </span>
                      </div>
                    </div>

                    {/* Miktar ve Silme Butonları */}
                    <div
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "16px",
                      }}
                    >
                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          border: "1px solid #cbd5e1",
                          borderRadius: "6px",
                        }}
                      >
                        <button
                          onClick={() =>
                            updateQuantity(item.id, item.quantity - 1)
                          }
                          style={{
                            padding: "4px 10px",
                            border: "none",
                            background: "transparent",
                            cursor: "pointer",
                            fontSize: "14px",
                          }}
                        >
                          -
                        </button>
                        <span style={{ padding: "0 8px", fontWeight: "bold" }}>
                          {item.quantity}
                        </span>
                        <button
                          onClick={() =>
                            updateQuantity(item.id, item.quantity + 1)
                          }
                          style={{
                            padding: "4px 10px",
                            border: "none",
                            background: "transparent",
                            cursor: "pointer",
                            fontSize: "14px",
                          }}
                        >
                          +
                        </button>
                      </div>

                      <strong
                        style={{
                          minWidth: "80px",
                          textAlign: "right",
                          color: "#0f766e",
                        }}
                      >
                        {item.price * item.quantity} TL
                      </strong>

                      {/* 🔴 SİLME BUTONU */}
                      <button
                        onClick={() => removeFromCart(item.id)}
                        title="Sepetten Sil"
                        style={{
                          border: "none",
                          background: "#fee2e2",
                          color: "#b91c1c",
                          borderRadius: "6px",
                          padding: "6px",
                          cursor: "pointer",
                          display: "flex",
                          alignItems: "center",
                        }}
                      >
                        <Trash2 size={16} />
                      </button>
                    </div>
                  </div>
                ))}
              </div>

              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  borderTop: "2px solid #e2e8f0",
                  marginTop: "20px",
                  paddingTop: "16px",
                }}
              >
                <span style={{ fontSize: "18px", fontWeight: "bold" }}>
                  Toplam Tutar:
                </span>
                <span
                  style={{
                    fontSize: "22px",
                    fontWeight: "bold",
                    color: "#10b981",
                  }}
                >
                  {totalAmount} TL
                </span>
              </div>

              {errorMessage && (
                <div
                  style={{
                    background: "#fee2e2",
                    color: "#b91c1c",
                    padding: "10px",
                    borderRadius: "6px",
                    marginTop: "16px",
                  }}
                >
                  {errorMessage}
                </div>
              )}

              <button
                onClick={handleCreateOrder}
                disabled={isSubmitting}
                style={{
                  width: "100%",
                  marginTop: "20px",
                  background: "#2563eb",
                  color: "#fff",
                  border: "none",
                  padding: "14px",
                  borderRadius: "8px",
                  fontSize: "16px",
                  fontWeight: "bold",
                  cursor: isSubmitting ? "not-allowed" : "pointer",
                }}
              >
                {isSubmitting
                  ? "Sipariş İşleniyor..."
                  : "Siparişi Onayla ve Öde"}
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
