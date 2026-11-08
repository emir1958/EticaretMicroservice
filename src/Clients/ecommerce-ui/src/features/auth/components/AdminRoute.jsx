import { Navigate, Outlet, useLocation } from "react-router-dom";
import { ShieldAlert } from "lucide-react";
import { useAuth } from "../AuthContext";
import Button from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import styles from "./AdminRoute.module.css";

// Yönetim paneli route'larını saran guard route (bkz. ProtectedRoute).
// Not: Bu yalnızca arayüzü gizler. Asıl yetki kontrolü backend'de ([Authorize(Roles = "Admin")]).
export default function AdminRoute() {
  const { isAuthenticated, isAdmin } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/admin/login" state={{ from: location }} replace />;
  }

  // Render saf olmalı: alert() gibi yan etkiler burada yapılmaz, durum ekranda gösterilir.
  if (!isAdmin) {
    return (
      <div className={styles.forbidden}>
        <EmptyState
          icon={ShieldAlert}
          title="Bu sayfaya erişim yetkiniz yok"
          text="Yönetim paneli yalnızca yönetici hesaplarına açıktır."
          action={<Button to="/">Mağazaya dön</Button>}
        />
      </div>
    );
  }

  return <Outlet />;
}
