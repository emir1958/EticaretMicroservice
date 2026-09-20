import React, { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import api from "../services/api";
import { ArrowLeft, User, MapPin, Package } from "lucide-react";

export default function AdminOrderDetail() {
  const { id } = useParams();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get(`/api/Orders/${id}`)
      .then((res) => {
        setOrder(res.data?.data || res.data);
        setLoading(false);
      })
      .catch((err) => {
        console.error("Sipariş detayı alınamadı", err);
        setLoading(false);
      });
  }, [id]);

  if (loading)
    return (
      <div style={{ padding: "40px", textAlign: "center" }}>Yükleniyor...</div>
    );
  if (!order)
    return (
      <div style={{ padding: "40px", textAlign: "center" }}>
        Sipariş bulunamadı.
      </div>
    );

  const items = order.orderItems || [];
  const address = order.address || {};

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
        to="/admin/orders"
        style={{
          display: "inline-flex",
          alignItems: "center",
          gap: "6px",
          textDecoration: "none",
          color: "#2563eb",
          marginBottom: "20px",
        }}
      >
        <ArrowLeft size={18} /> Siparişlere Dön
      </Link>

      <div
        style={{
          background: "#fff",
          border: "1px solid #e2e8f0",
          borderRadius: "12px",
          padding: "24px",
          marginBottom: "24px",
        }}
      >
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            borderBottom: "1px solid #e2e8f0",
            paddingBottom: "16px",
            marginBottom: "16px",
          }}
        >
          <div>
            <h2 style={{ margin: "0 0 4px 0" }}>Sipariş #{order.id}</h2>
            <span style={{ color: "#64748b", fontSize: "14px" }}>
              {new Date(order.createdAt).toLocaleString("tr-TR")}
            </span>
          </div>
          <div style={{ textAlign: "right" }}>
            <span
              style={{
                background: "#dcfce7",
                color: "#166534",
                padding: "6px 12px",
                borderRadius: "20px",
                fontWeight: "bold",
                fontSize: "14px",
              }}
            >
              {order.orderStatus || order.status || "Tamamlandı"}
            </span>
            <div
              style={{ marginTop: "8px", fontSize: "20px", fontWeight: "bold" }}
            >
              {order.totalPrice || order.totalAmount} TL
            </div>
          </div>
        </div>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gap: "24px",
          }}
        >
          <div
            style={{
              background: "#f8fafc",
              padding: "16px",
              borderRadius: "8px",
            }}
          >
            <h3
              style={{
                margin: "0 0 12px 0",
                fontSize: "15px",
                display: "flex",
                alignItems: "center",
                gap: "6px",
              }}
            >
              <User size={16} /> Müşteri Bilgileri
            </h3>
            <p style={{ margin: "0 0 4px 0", fontSize: "14px" }}>
              <strong>Müşteri ID:</strong> {order.buyerId}
            </p>
          </div>
          <div
            style={{
              background: "#f8fafc",
              padding: "16px",
              borderRadius: "8px",
            }}
          >
            <h3
              style={{
                margin: "0 0 12px 0",
                fontSize: "15px",
                display: "flex",
                alignItems: "center",
                gap: "6px",
              }}
            >
              <MapPin size={16} /> Teslimat Adresi
            </h3>
            <p style={{ margin: "0", fontSize: "14px", color: "#475569" }}>
              {address.line} {address.street}
              <br />
              {address.district} / {address.city}
              <br />
              {address.zipCode}
            </p>
          </div>
        </div>
      </div>

      <h3 style={{ display: "flex", alignItems: "center", gap: "8px" }}>
        <Package size={20} /> Sipariş Edilen Ürünler
      </h3>
      <div
        style={{
          background: "#fff",
          border: "1px solid #e2e8f0",
          borderRadius: "12px",
          overflow: "hidden",
        }}
      >
        {items.map((item, index) => (
          <div
            key={index}
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              padding: "16px",
              borderBottom:
                index !== items.length - 1 ? "1px solid #f1f5f9" : "none",
            }}
          >
            <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
              <div
                style={{
                  width: "50px",
                  height: "50px",
                  background: "#f1f5f9",
                  borderRadius: "8px",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                <Package size={24} color="#94a3b8" />
              </div>
              <div>
                <h4 style={{ margin: "0 0 4px 0" }}>
                  {item.productName || item.productId}
                </h4>
                <span style={{ color: "#64748b", fontSize: "13px" }}>
                  Birim Fiyat: {item.price} TL
                </span>
              </div>
            </div>
            <div style={{ textAlign: "right" }}>
              <div
                style={{
                  fontSize: "14px",
                  color: "#475569",
                  marginBottom: "4px",
                }}
              >
                Miktar: <strong>{item.quantity}</strong>
              </div>
              <div style={{ fontWeight: "bold", color: "#0f766e" }}>
                {item.price * item.quantity} TL
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
