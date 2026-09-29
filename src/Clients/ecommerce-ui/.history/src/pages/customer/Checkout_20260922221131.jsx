import React, { useState } from "react";
import { useCart } from "../../context/CartContext";
import { useAuth } from "../../context/AuthContext";
import { useOrderHub } from "../../hooks/useOrderHub";
import api from "../../services/api";
import { Link, useNavigate } from "react-router-dom";
import {
  ArrowLeft,
  CheckCircle,
  XCircle,
  Clock,
  CreditCard,
  ShieldCheck,
  AlertTriangle,
} from "lucide-react";

export default function Checkout() {
  const { cart, totalAmount, clearCart } = useCart();
  const { user } = useAuth();
  const { orderStatus } = useOrderHub();
  const navigate = useNavigate();
  const { user, isAdmin } = useAuth();
  // Kart Form State'leri
  const [cardHolder, setCardHolder] = useState("");
  const [cardNumber, setCardNumber] = useState("");
  const [expiry, setExpiry] = useState("");
  const [cvv, setCvv] = useState("");

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [createdOrderId, setCreatedOrderId] = useState(null);
  const [errorMessage, setErrorMessage] = useState(null);

  const formatCardNumber = (val) => {
    const raw = val.replace(/\D/g, "").slice(0, 16);
    return raw.match(/.{1,4}/g)?.join(" ") || raw;
  };

  const formatExpiry = (val) => {
    let raw = val.replace(/\D/g, "").slice(0, 4);
    if (raw.length >= 3) return `${raw.slice(0, 2)}/${raw.slice(2)}`;
    return raw;
  };

  const handleCreateOrder = async (e) => {
    e.preventDefault();
    if (cart.length === 0) return;

    const rawCard = cardNumber.replace(/\s/g, "");
    if (rawCard.length !== 16 || cvv.length < 3 || expiry.length !== 5) {
      setErrorMessage("Lütfen kart bilgilerinizi eksiksiz ve doğru girin.");
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);
    // Kart numarasının son 4 hanesini alıp token'a ekliyoruz:
    const last4 = cleanCard.slice(-4);
    const paymentToken = `tok_card_${last4}`;

    const payload = {
      buyerId: user?.id,
      address: {
        city: "İstanbul",
        district: "Kadıköy",
        street: "Moda Caddesi",
        zipCode: "34710",
        line: "No:12 Daire:4",
      },
      payment: {
        paymentToken: paymentToken,
      },
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
      setIsSubmitting(false);
    }
  };

  return (
    <div
      style={{
        maxWidth: "800px",
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
              orderStatus.status === "Completed" ||
              orderStatus.status === "Tamamlandı" ? (
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "12px",
                    marginTop: "12px",
                  }}
                >
                  <CheckCircle color="#10b981" size={32} />
                  <div>
                    <strong style={{ color: "#10b981" }}>
                      Ödeme ve Sipariş Başarıyla Tamamlandı!
                    </strong>
                    <p
                      style={{ margin: 0, fontSize: "14px", color: "#64748b" }}
                    >
                      Stok rezerve edildi ve ödeme başarıyla onaylandı.
                    </p>
                  </div>
                </div>
              ) : (
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "12px",
                    marginTop: "12px",
                  }}
                >
                  <XCircle color="#ef4444" size={32} />
                  <div>
                    <strong style={{ color: "#ef4444" }}>
                      Sipariş İptal Edildi
                    </strong>
                    <p
                      style={{ margin: 0, fontSize: "14px", color: "#64748b" }}
                    >
                      {orderStatus.message ||
                        "Telafi işlemi (compensation) yapıldı."}
                    </p>
                  </div>
                </div>
              )
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
        <form onSubmit={handleCreateOrder}>
          {errorMessage && (
            <div
              style={{
                background: "#fee2e2",
                border: "1px solid #ef4444",
                color: "#b91c1c",
                padding: "12px",
                borderRadius: "8px",
                marginBottom: "16px",
                display: "flex",
                alignItems: "center",
                gap: "8px",
              }}
            >
              <AlertTriangle size={18} /> {errorMessage}
            </div>
          )}

          {/* Kart Bilgileri Alanı */}
          <div
            style={{
              background: "#fff",
              border: "1px solid #e2e8f0",
              borderRadius: "10px",
              padding: "20px",
              marginBottom: "20px",
            }}
          >
            <h4
              style={{
                margin: "0 0 16px 0",
                display: "flex",
                alignItems: "center",
                gap: "8px",
                color: "#334155",
              }}
            >
              <CreditCard size={20} color="#2563eb" /> Kart Bilgileri
              (Simülasyon)
            </h4>

            <div style={{ marginBottom: "14px" }}>
              <label
                style={{
                  display: "block",
                  fontSize: "13px",
                  color: "#64748b",
                  marginBottom: "4px",
                }}
              >
                Kart Üzerindeki İsim
              </label>
              <input
                type="text"
                required
                placeholder="Ad Soyad"
                value={cardHolder}
                onChange={(e) => setCardHolder(e.target.value)}
                style={{
                  width: "100%",
                  padding: "10px",
                  borderRadius: "6px",
                  border: "1px solid #cbd5e1",
                  boxSizing: "border-box",
                }}
              />
            </div>

            <div style={{ marginBottom: "14px" }}>
              <label
                style={{
                  display: "block",
                  fontSize: "13px",
                  color: "#64748b",
                  marginBottom: "4px",
                }}
              >
                Kart Numarası
              </label>
              <input
                type="text"
                required
                placeholder="0000 0000 0000 0000"
                value={cardNumber}
                onChange={(e) =>
                  setCardNumber(formatCardNumber(e.target.value))
                }
                style={{
                  width: "100%",
                  padding: "10px",
                  borderRadius: "6px",
                  border: "1px solid #cbd5e1",
                  boxSizing: "border-box",
                  letterSpacing: "2px",
                }}
              />
              <small style={{ color: "#94a3b8", fontSize: "11px" }}>
                💡 Demo: Sonu <strong>0000</strong> girilirse başarısız ödeme
                senaryosu simüle edilir.
              </small>
            </div>

            <div
              style={{
                display: "grid",
                gridTemplateColumns: "1fr 1fr",
                gap: "12px",
              }}
            >
              <div>
                <label
                  style={{
                    display: "block",
                    fontSize: "13px",
                    color: "#64748b",
                    marginBottom: "4px",
                  }}
                >
                  Son Kullanma (AA/YY)
                </label>
                <input
                  type="text"
                  required
                  placeholder="AA/YY"
                  value={expiry}
                  onChange={(e) => setExpiry(formatExpiry(e.target.value))}
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
                    color: "#64748b",
                    marginBottom: "4px",
                  }}
                >
                  CVV
                </label>
                <input
                  type="password"
                  required
                  maxLength={3}
                  placeholder="•••"
                  value={cvv}
                  onChange={(e) => setCvv(e.target.value.replace(/\D/g, ""))}
                  style={{
                    width: "100%",
                    padding: "10px",
                    borderRadius: "6px",
                    border: "1px solid #cbd5e1",
                    boxSizing: "border-box",
                  }}
                />
              </div>
            </div>
          </div>

          {/* Toplam Tutar ve Sipariş Butonu */}
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              borderTop: "2px solid #e2e8f0",
              paddingTop: "16px",
              marginBottom: "20px",
            }}
          >
            <span style={{ fontSize: "18px", fontWeight: "bold" }}>
              Ödenecek Tutar:
            </span>
            <span
              style={{ fontSize: "22px", fontWeight: "bold", color: "#10b981" }}
            >
              {totalAmount} TL
            </span>
          </div>

          <button
            type="submit"
            disabled={isSubmitting || cart.length === 0}
            style={{
              width: "100%",
              background:
                isSubmitting || cart.length === 0 ? "#94a3b8" : "#2563eb",
              color: "#fff",
              border: "none",
              padding: "14px",
              borderRadius: "8px",
              fontSize: "16px",
              fontWeight: "bold",
              cursor:
                isSubmitting || cart.length === 0 ? "not-allowed" : "pointer",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              gap: "8px",
            }}
          >
            <ShieldCheck size={20} />
            {isSubmitting
              ? "Sipariş ve Ödeme İşleniyor..."
              : "Ödemeyi Tamamla ve Siparişi Ver"}
          </button>
        </form>
      )}
    </div>
  );
}
