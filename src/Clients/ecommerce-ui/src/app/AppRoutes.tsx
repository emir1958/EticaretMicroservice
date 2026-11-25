import { lazy, Suspense } from "react";
import { Route, Routes } from "react-router-dom";
import CustomerLayout from "./layouts/CustomerLayout";
import NotFoundPage from "./NotFoundPage";
import PageLoader from "../shared/components/PageLoader";
import ProtectedRoute from "../features/auth/components/ProtectedRoute";
import AdminRoute from "../features/auth/components/AdminRoute";
import AuthPage from "../features/auth/pages/AuthPage";
import HomePage from "../features/catalog/pages/HomePage";
import ProductListPage from "../features/catalog/pages/ProductListPage";
import ProductDetailPage from "../features/catalog/pages/ProductDetailPage";
import CheckoutPage from "../features/orders/pages/CheckoutPage";
import MyOrdersPage from "../features/orders/pages/MyOrdersPage";
import MyOrderDetailPage from "../features/orders/pages/MyOrderDetailPage";
import AccountPage from "../features/account/pages/AccountPage";
import AdminLoginPage from "../features/admin/pages/AdminLoginPage";

// Yönetim paneli ayrı bir JS parçası olarak yalnızca /admin açıldığında indirilir; müşteri bu kodu hiç yüklemez
const AdminLayout = lazy(() => import("./layouts/AdminLayout"));
const AdminDashboardPage = lazy(() => import("../features/admin/pages/AdminDashboardPage"));
const AdminProductsPage = lazy(() => import("../features/admin/pages/AdminProductsPage"));
const AdminCategoriesPage = lazy(() => import("../features/admin/pages/AdminCategoriesPage"));
const AdminStockPage = lazy(() => import("../features/admin/pages/AdminStockPage"));
const AdminOrdersPage = lazy(() => import("../features/admin/pages/AdminOrdersPage"));
const AdminOrderDetailPage = lazy(() => import("../features/admin/pages/AdminOrderDetailPage"));
const AdminCustomersPage = lazy(() => import("../features/admin/pages/AdminCustomersPage"));
const AdminPromotionsPage = lazy(() => import("../features/admin/pages/AdminPromotionsPage"));
const AdminSystemPage = lazy(() => import("../features/admin/pages/AdminSystemPage"));

// Layout'lar ve guard'lar (ProtectedRoute, AdminRoute) path'siz layout route olarak ağaçta bir kez yazılır
export default function AppRoutes() {
  return (
    <Routes>
      {/* Mağaza: ortak header/footer ve sepet çekmecesi */}
      <Route element={<CustomerLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/products" element={<ProductListPage />} />
        <Route path="/product/:id" element={<ProductDetailPage />} />
        <Route path="/auth" element={<AuthPage />} />

        {/* Giriş gerektirenler */}
        <Route element={<ProtectedRoute />}>
          <Route path="/checkout" element={<CheckoutPage />} />
          <Route path="/account" element={<AccountPage />} />
          <Route path="/orders" element={<MyOrdersPage />} />
          <Route path="/orders/:id" element={<MyOrderDetailPage />} />
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Route>

      {/* Yönetim paneli */}
      <Route path="/admin/login" element={<AdminLoginPage />} />
      <Route path="/admin" element={<AdminRoute />}>
        <Route
          element={
            <Suspense fallback={<PageLoader />}>
              <AdminLayout />
            </Suspense>
          }
        >
          <Route index element={<AdminDashboardPage />} />
          <Route path="orders" element={<AdminOrdersPage />} />
          <Route path="orders/:id" element={<AdminOrderDetailPage />} />
          <Route path="customers" element={<AdminCustomersPage />} />
          <Route path="promotions" element={<AdminPromotionsPage />} />
          <Route path="products" element={<AdminProductsPage />} />
          <Route path="categories" element={<AdminCategoriesPage />} />
          <Route path="stock" element={<AdminStockPage />} />
          <Route path="system" element={<AdminSystemPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
