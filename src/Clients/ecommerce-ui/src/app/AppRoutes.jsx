import { Navigate, Route, Routes } from "react-router-dom";
import CustomerLayout from "./layouts/CustomerLayout";
import AdminLayout from "./layouts/AdminLayout";
import ProtectedRoute from "../features/auth/components/ProtectedRoute";
import AdminRoute from "../features/auth/components/AdminRoute";
import AuthPage from "../features/auth/pages/AuthPage";
import ProductListPage from "../features/catalog/pages/ProductListPage";
import ProductDetailPage from "../features/catalog/pages/ProductDetailPage";
import CheckoutPage from "../features/orders/pages/CheckoutPage";
import MyOrdersPage from "../features/orders/pages/MyOrdersPage";
import MyOrderDetailPage from "../features/orders/pages/MyOrderDetailPage";
import AdminLoginPage from "../features/admin/pages/AdminLoginPage";
import AdminProductsPage from "../features/admin/pages/AdminProductsPage";
import AdminOrdersPage from "../features/admin/pages/AdminOrdersPage";
import AdminOrderDetailPage from "../features/admin/pages/AdminOrderDetailPage";

// Uygulamanın tüm adresleri tek yerde. Yeni sayfa: features/<feature>/pages altına yaz, buraya ekle.
//
// path'i olmayan <Route element={...}> bir "layout route"tur: adres eklemez, çocuklarını sarar.
// İçindeki bileşen <Outlet /> ile eşleşen çocuğun yerini belirler. Bu yüzden layout'lar ve guard'lar
// (ProtectedRoute, AdminRoute) sayfaların içine değil, route ağacına bir kez yazılır.
export default function AppRoutes() {
  return (
    <Routes>
      {/* Mağaza: ortak header/footer ve sepet çekmecesi */}
      <Route element={<CustomerLayout />}>
        <Route path="/" element={<ProductListPage />} />
        <Route path="/product/:id" element={<ProductDetailPage />} />
        <Route path="/auth" element={<AuthPage />} />

        {/* Giriş gerektirenler */}
        <Route element={<ProtectedRoute />}>
          <Route path="/checkout" element={<CheckoutPage />} />
          <Route path="/orders" element={<MyOrdersPage />} />
          <Route path="/orders/:id" element={<MyOrderDetailPage />} />
        </Route>
      </Route>

      {/* Yönetim paneli */}
      <Route path="/admin/login" element={<AdminLoginPage />} />
      <Route path="/admin" element={<AdminRoute />}>
        <Route element={<AdminLayout />}>
          <Route index element={<Navigate to="orders" replace />} />
          <Route path="orders" element={<AdminOrdersPage />} />
          <Route path="orders/:id" element={<AdminOrderDetailPage />} />
          <Route path="products" element={<AdminProductsPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
