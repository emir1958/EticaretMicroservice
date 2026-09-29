// CartDrawer.jsx içine useAuth hook'unu dahil edin:
import { useAuth } from "../context/AuthContext";

// Bileşen içinde:
const { isAuthenticated } = useAuth();

// Butonun onClick'i:
<button
  onClick={() => {
    setIsCartOpen(false);
    if (!isAuthenticated) {
      navigate("/auth?redirect=/checkout");
    } else {
      navigate("/checkout");
    }
  }}
  style={{
    width: "100%",
    padding: "14px",
    background: "#2563eb",
    color: "#fff",
    border: "none",
    borderRadius: "8px",
    fontSize: "16px",
    fontWeight: "bold",
    cursor: "pointer",
  }}
>
  Siparişi Tamamla
</button>;
