import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../AuthContext";

// Giriş gerektiren route'ları saran "guard route". Router'da çocuk route'ların üstüne konur:
//
//   <Route element={<ProtectedRoute />}>
//     <Route path="/checkout" element={<CheckoutPage />} />
//   </Route>
//
// Giriş yoksa /auth'a yönlendirir ve gelinen yeri state ile taşır (AuthPage girişten sonra oraya döner).
export default function ProtectedRoute() {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/auth" state={{ from: location }} replace />;
  }

  return <Outlet />;
}
