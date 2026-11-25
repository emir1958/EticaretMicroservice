import { Navigate, Outlet, useLocation } from "react-router-dom";
import { ShieldAlert } from "lucide-react";
import { useAuth } from "../AuthContext";
import { ButtonLink } from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import PageLoader from "../../../shared/components/PageLoader";
import styles from "./AdminRoute.module.css";

export default function AdminRoute() {
  const { isAuthenticated, isAdmin, isLoading } = useAuth();
  const location = useLocation();

  // Oturum cookie'den geri yüklenirken yönlendirme yapılmaz (bkz. ProtectedRoute)
  if (isLoading) return <PageLoader />;

  if (!isAuthenticated) {
    return <Navigate to="/admin/login" state={{ from: location }} replace />;
  }

  if (!isAdmin) {
    return (
      <div className={styles.forbidden}>
        <EmptyState
          icon={ShieldAlert}
          title="Bu sayfaya erişim yetkiniz yok"
          text="Yönetim paneli yalnızca yönetici hesaplarına açıktır."
          action={<ButtonLink to="/">Mağazaya dön</ButtonLink>}
        />
      </div>
    );
  }

  return <Outlet />;
}
