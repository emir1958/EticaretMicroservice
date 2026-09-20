import React, { useEffect, useState } from "react";
import api from "../services/api";
import { Link } from "react-router-dom";
import { ArrowLeft, Eye } from "lucide-react";

export default function AdminOrders() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get("/api/Orders")
      .then((res) => {
        setOrders(Array.isArray(res.data) ? res.data : res.data?.data || []);
        setLoading(false);
      })
      .catch((err) => {
        console.error("Siparişler getirilemedi:", err);
        setLoading(false);
      });
  }, []);

  return (
    <div
      style={{
        maxWidth: "1000px",
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

      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: "20px",
        }}
      >
        <h2 style={{ margin: 0 }}>📦 Sipariş Yönetimi</h2>
      </div>

      {loading ? (
        <p>Yükleniyor...</p>
      ) : (
        <table
          style={{
            width: "100%",
            borderCollapse: "collapse",
            background: "#fff",
            border: "1px solid #e2e8f0",
            borderRadius: "8px",
            overflow: "hidden",
          }}
        >
          <thead>
            <tr
              style={{
                background: "#f1f5f9",
                textAlign: "left",
                fontSize: "13px",
                color: "#475569",
              }}
            >
              <th style={{ padding: "12px" }}>Sipariş ID</th>
              <th style={{ padding: "12px" }}>Müşteri ID</th>
              <th style={{ padding: "12px" }}>Tarih</th>
              <th style={{ padding: "12px" }}>Tutar</th>
              <th style={{ padding: "12px" }}>Durum</th>
              <th style={{ padding: "12px", textAlign: "right" }}>Detay</th>
            </tr>
          </thead>
          <tbody>
            {orders.length === 0 ? (
              <tr>
                <td
                  colSpan={6}
                  style={{ padding: "24px", textAlign: "center" }}
                >
                  Sipariş bulunamadı.
                </td>
              </tr>
            ) : (
              orders.map((o) => (
                <tr
                  key={o.id}
                  style={{
                    borderBottom: "1px solid #f1f5f9",
                    fontSize: "14px",
                  }}
                >
                  <td style={{ padding: "12px", fontWeight: "600" }}>
                    #{o.id}
                  </td>
                  <td style={{ padding: "12px" }}>
                    {o.buyerId?.substring(0, 8)}...
                  </td>
                  <td style={{ padding: "12px", color: "#64748b" }}>
                    {new Date(o.createdAt || new Date()).toLocaleDateString(
                      "tr-TR",
                    )}
                  </td>
                  <td style={{ padding: "12px", fontWeight: "bold" }}>
                    {o.totalPrice || o.totalAmount} TL
                  </td>
                  <td style={{ padding: "12px" }}>
                    <span
                      style={{
                        background: "#e0e7ff",
                        color: "#4338ca",
                        padding: "4px 8px",
                        borderRadius: "4px",
                        fontSize: "12px",
                        fontWeight: "bold",
                      }}
                    >
                      {o.orderStatus || o.status || "Hazırlanıyor"}
                    </span>
                  </td>
                  <td style={{ padding: "12px", textAlign: "right" }}>
                    <Link
                      to={`/admin/orders/${o.id}`}
                      style={{
                        background: "#2563eb",
                        color: "#fff",
                        padding: "6px 12px",
                        borderRadius: "6px",
                        textDecoration: "none",
                        fontSize: "13px",
                        display: "inline-flex",
                        alignItems: "center",
                        gap: "4px",
                      }}
                    >
                      <Eye size={14} /> İncele
                    </Link>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      )}
    </div>
  );
}
