import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "../features/auth/AuthContext";
import { CartProvider } from "../features/cart/CartContext";
import { ToastProvider } from "../shared/toast/ToastContext";
import AppRoutes from "./AppRoutes";

export default function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <AuthProvider>
          <CartProvider>
            <AppRoutes />
          </CartProvider>
        </AuthProvider>
      </ToastProvider>
    </BrowserRouter>
  );
}
