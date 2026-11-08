import React, { useEffect, useState } from "react";
import api from "../../services/api";
import { Link } from "react-router-dom";
import { Eye } from "lucide-react";
import AdminLayout from "../../components/AdminLayout"; // 🟢 Eklendi

export default function AdminOrders() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null); // Hata izleme eklendi

  useEffect(() => {
    api
      .get("/api/Orders")
      .then((res) => {
        setOrders(Array.isArray(res.data) ? res.data : res.data?.data || []);
        setLoading(false);
      })
      .catch((err) => {
        console.error("Siparişler getirilemedi:", err);
        setError(
          err.response?.data?.message ||
            err.message ||
            "Siparişlere erişim yetkiniz yok veya servis kapalı.",
        );
        setLoading(false);
      });
  }, []);

  return (
    <AdminLayout>
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: "20px",
        }}
      >
        <h2 style={{ margin: 0, color: "#0f172a" }}>📦 Sipariş Yönetimi</h2>
      </div>

      {loading ? (
        <p>Yükleniyor...</p>
      ) : error ? (
        <p style={{ color: "red" }}>{error}</p>
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
                    {new Date(
                      o.createdDate || o.CreatedDate || o.createdAt,
                    ).toLocaleDateString("tr-TR")}
                  </td>
                  <td style={{ padding: "12px", fontWeight: "bold" }}>
                    {o.totalPrice || o.totalAmount} TL
                  </td>
                  {/* Tarih Kolonu: createdDate veya CreatedDate uyumlu */}
                  <td style={{ padding: "12px", color: "#64748b" }}>
                    {new Date(
                      o.createdDate || o.CreatedDate || o.createdAt,
                    ).toLocaleDateString("tr-TR")}
                  </td>

                  {/* Durum Kolonu: Canceled, Completed, Suspend renklendirmesiyle */}
                  <td style={{ padding: "12px" }}>
                    <span
                      style={{
                        background:
                          o.orderStatus === "Completed" || o.orderStatus === "3"
                            ? "#dcfce7"
                            : o.orderStatus === "Canceled" ||
                                o.orderStatus === "2"
                              ? "#fee2e2"
                              : "#e0e7ff",
                        color:
                          o.orderStatus === "Completed" || o.orderStatus === "3"
                            ? "#15803d"
                            : o.orderStatus === "Canceled" ||
                                o.orderStatus === "2"
                              ? "#b91c1c"
                              : "#4338ca",
                        padding: "4px 8px",
                        borderRadius: "4px",
                        fontSize: "12px",
                        fontWeight: "bold",
                      }}
                    >
                      {o.orderStatus === "Completed" || o.orderStatus === "3"
                        ? "Tamamlandı"
                        : o.orderStatus === "Canceled" || o.orderStatus === "2"
                          ? "İptal Edildi"
                          : "Beklemede"}
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
    </AdminLayout>
  );
}
