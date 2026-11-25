import { Navigate, Outlet, useLocation } from "react-router-dom";
import PageLoader from "../../../shared/components/PageLoader";
import { useAuth } from "../AuthContext";

// Giriş yoksa /auth'a yönlendirir; gelinen yer state ile taşınır, AuthPage girişten sonra oraya döner
export default function ProtectedRoute() {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();

  // Sayfa yeni açıldıysa oturum cookie'den geri yükleniyor; bitmeden /auth'a yönlendirmek giriş yapmış kullanıcıyı atardı
  if (isLoading) return <PageLoader />;

  if (!isAuthenticated) {
    return <Navigate to="/auth" state={{ from: location }} replace />;
  }

  return <Outlet />;
}
