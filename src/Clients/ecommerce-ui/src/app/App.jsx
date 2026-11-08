import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "../features/auth/AuthContext";
import { CartProvider } from "../features/cart/CartContext";
import AppRoutes from "./AppRoutes";

// Uygulamanın kökü: yalnızca global provider'lar burada.
// Sıra önemli: CartProvider kullanıcıyı bilmek için AuthProvider'ın içinde olmalı.
// Router en dışta: ileride provider'lar da navigasyon (useNavigate) kullanabilir.
export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <CartProvider>
          <AppRoutes />
        </CartProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}
