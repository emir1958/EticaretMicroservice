import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export default function ProtectedRoute({ children }) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    // Giriş yapılmamışsa login sayfasına yönlendir, geldiği yeri state ile aktar
    return <Navigate to="/auth" state={{ from: location }} replace />;
  }

  return children;
}
