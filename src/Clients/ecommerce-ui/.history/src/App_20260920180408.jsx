import React from "react";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { CartProvider } from "./context/CartContext";
import ProductList from "./pages/ProductList";
import ProductDetail from "./pages/ProductDetail"; // 🟢 Eklendi
import Checkout from "./pages/Checkout";
import Auth from "./pages/Auth";
import AdminProduct from "./pages/AdminProduct";
import AdminOrders from "./pages/AdminOrders"; // 🟢 Eklendi
import AdminOrderDetail from "./pages/AdminOrderDetail"; // 🟢 Eklendi
import ProtectedRoute from "./components/ProtectedRoute";
import CartDrawer from "./components/CartDrawer"; // 🟢 Eklendi

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
