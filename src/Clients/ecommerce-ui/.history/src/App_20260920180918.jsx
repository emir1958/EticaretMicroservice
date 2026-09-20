import React from "react";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { CartProvider } from "./context/CartContext";
import ProductList from "./pages/customer/ProductList";
import ProductDetail from "./pages/customer/ProductDetail";
import Checkout from "./pages/customer/Checkout";
import Auth from "./pages/auth/Auth";
import AdminProduct from "./pages/admin/AdminProduct";
import AdminOrders from "./pages/admin/AdminOrders";
import AdminOrderDetail from "./pages/admin/AdminOrderDetail";
import ProtectedRoute from "./components/ProtectedRoute";
import CartDrawer from "./components/CartDrawer";

export default function App() {
  return (
    <AuthProvider>
      <CartProvider>
        <BrowserRouter>
          {/* 🟢 Tüm Sayfalarda Çalışan Ortak Sepet Popup'ı */}
          <CartDrawer />

          <Routes>
            <Route path="/" element={<ProductList />} />
            <Route path="/product/:id" element={<ProductDetail />} />{" "}
            {/* 🟢 Eklendi */}
            <Route path="/auth" element={<Auth />} />
            {/* Admin Rotaları */}
            <Route path="/admin/products" element={<AdminProduct />} />
            <Route path="/admin/orders" element={<AdminOrders />} />{" "}
            {/* 🟢 Eklendi */}
            <Route
              path="/admin/orders/:id"
              element={<AdminOrderDetail />}
            />{" "}
            {/* 🟢 Eklendi */}
            <Route
              path="/checkout"
              element={
                <ProtectedRoute>
                  <Checkout />
                </ProtectedRoute>
              }
            />
          </Routes>
        </BrowserRouter>
      </CartProvider>
    </AuthProvider>
  );
}
