import React from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export default function AdminRoute({ children }) {
  const { isAuthenticated, isAdmin, loading } = useAuth();

  if (loading) {
    return (
      <div style={{ padding: "40px", textAlign: "center" }}>
        Yetki kontrol ediliyor...
      </div>
    );
  }

  // Giriş yapmamışsa Admin Login'e yönlendir
  if (!isAuthenticated) {
    return <Navigate to="/admin/login" replace />;
  }

  // Giriş yapmış ama Admin değilse ana sayfaya yönlendir
  if (!isAdmin) {
    alert("Bu sayfaya erişim yetkiniz bulunmamaktadır (Yalnızca Admin).");
    return <Navigate to="/" replace />;
  }

  return children;
}
