import React, { useState } from "react";
import { useCart } from "../context/CartContext";
import { useOrderHub } from "../hooks/useOrderHub";
import api from "../services/api";
import { Link } from "react-router-dom";
import { ArrowLeft, CheckCircle, XCircle, Clock } from "lucide-react";

export default function Checkout() {
  const { cart, totalAmount, clearCart } = useCart();
  const { orderStatus } = useOrderHub();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [createdOrderId, setCreatedOrderId] = useState(null);
  const [errorMessage, setErrorMessage] = useState(null);

  const handleCreateOrder = async () => {
    if (cart.length === 0) return;
    setIsSubmitting(true);
    setErrorMessage(null);

    const payload = {
      buyerId: "user-frontend-test",
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
      orderItems: cart.map((item) => ({
        productId: item.id,
        quantity: item.quantity,
      })),
    };

    try {
      // YARP Gateway üzerinden Order.WebApi /api/orders rotasına gider
      const response = await api.post("/api/orders", payload);
      setCreatedOrderId(response.data);
      clearCart();
    } catch (err) {
      console.error("Sipariş oluşturma hatası:", err);
      setErrorMessage(
        err.response?.data?.detail ||
          "Sipariş oluşturulurken bir hata meydana geldi.",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div
      style={{
        maxWidth: "700px",
        margin: "0 auto",
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

      <h2>Sipariş ve Ödeme</h2>

      {/* SİPARİŞ OLUŞTURULDUYSA: CANLI TAKİP PANELİ */}
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
            Event-Driven Saga mimarisi çalışıyor. Stok rezervasyonu ve ödeme
            adımları arka planda işleniyor...
          </p>

          <div
            style={{
              marginTop: "24px",
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
                        Stok rezerve edildi ve ödeme başarıyla çekildi.
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
                          "Yetersiz stok veya ödeme hatası nedeniyle telafi işlemi (compensation) yapıldı."}
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
                <span>Beklemede: Saga orkestrasyonu devam ediyor...</span>
              </div>
            )}
          </div>
        </div>
      ) : (
        /* SEPET LİSTESİ VE SİPARİŞ VER BUTONU */
        <div>
          {cart.length === 0 ? (
            <p>Sepetinizde ürün bulunmuyor.</p>
          ) : (
            <div>
              <ul style={{ listStyle: "none", padding: 0 }}>
                {cart.map((item) => (
                  <li
                    key={item.id}
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      padding: "12px 0",
                      borderBottom: "1px solid #e5e7eb",
                    }}
                  >
                    <span>
                      {item.name} (x{item.quantity})
                    </span>
                    <strong>{item.price * item.quantity} TL</strong>
                  </li>
                ))}
              </ul>
              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  marginTop: "16px",
                  fontSize: "18px",
                }}
              >
                <span>Toplam Tutar:</span>
                <span style={{ fontWeight: "bold", color: "#10b981" }}>
                  {totalAmount} TL
                </span>
              </div>

              {errorMessage && (
                <p style={{ color: "#ef4444", marginTop: "16px" }}>
                  {errorMessage}
                </p>
              )}

              <button
                onClick={handleCreateOrder}
                disabled={isSubmitting}
                style={{
                  width: "100%",
                  marginTop: "24px",
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
