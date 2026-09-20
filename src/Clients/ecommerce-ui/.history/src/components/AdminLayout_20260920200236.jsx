import React from "react";
import { Link, useLocation } from "react-router-dom";
import {
  LayoutDashboard,
  ShoppingCart,
  Package,
  LogOut,
  ArrowLeft,
} from "lucide-react";
import { useAuth } from "../context/AuthContext";

export default function AdminLayout({ children }) {
  const location = useLocation();
  const { logout } = useAuth();

  const menuItems = [
    {
      path: "/admin/products",
      name: "Ürün Yönetimi",
      icon: <Package size={18} />,
    },
    {
      path: "/admin/orders",
      name: "Siparişler",
      icon: <ShoppingCart size={18} />,
    },
  ];

  return (
    <div
      style={{
        display: "flex",
        minHeight: "100vh",
        background: "#f8fafc",
        fontFamily: "sans-serif",
      }}
    >
      <aside
        style={{
          width: "260px",
          background: "#0f172a",
          color: "#f8fafc",
          display: "flex",
          flexDirection: "column",
        }}
      >
        <div style={{ padding: "24px", borderBottom: "1px solid #1e293b" }}>
          <h2
            style={{
              margin: 0,
              fontSize: "18px",
              display: "flex",
              alignItems: "center",
              gap: "8px",
            }}
          >
            <LayoutDashboard size={20} /> Admin Panel
          </h2>
        </div>

        <nav style={{ flex: 1, padding: "16px 0" }}>
          {menuItems.map((item) => {
            const isActive = location.pathname.includes(item.path);
            return (
              <Link
                key={item.path}
                to={item.path}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "12px",
                  padding: "12px 24px",
                  textDecoration: "none",
                  color: isActive ? "#fff" : "#94a3b8",
                  background: isActive ? "#1e293b" : "transparent",
                  borderLeft: isActive
                    ? "4px solid #3b82f6"
                    : "4px solid transparent",
                  transition: "0.2s",
                }}
              >
                {item.icon}{" "}
                <span style={{ fontWeight: isActive ? "600" : "400" }}>
                  {item.name}
                </span>
              </Link>
            );
          })}
        </nav>

        <div
          style={{
            padding: "16px",
            borderTop: "1px solid #1e293b",
            display: "flex",
            flexDirection: "column",
            gap: "8px",
          }}
        >
          <Link
            to="/"
            style={{
              display: "flex",
              alignItems: "center",
              gap: "8px",
              color: "#94a3b8",
              textDecoration: "none",
              padding: "8px",
            }}
          >
            <ArrowLeft size={16} /> Mağazaya Dön
          </Link>
          <button
            onClick={logout}
            style={{
              display: "flex",
              alignItems: "center",
              gap: "8px",
              background: "transparent",
              color: "#ef4444",
              border: "none",
              cursor: "pointer",
              padding: "8px",
              textAlign: "left",
            }}
          >
            <LogOut size={16} /> Çıkış Yap
          </button>
        </div>
      </aside>

      <main style={{ flex: 1, padding: "32px", overflowY: "auto" }}>
        {children}
      </main>
    </div>
  );
}
