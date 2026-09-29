import React from "react";
import { useCart } from "../context/CartContext";
import { X, Trash2, ShoppingBag } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
export default function CartDrawer() {
  const {
    isCartOpen,
    setIsCartOpen,
    cart,
    removeFromCart,
    totalAmount,
    successMessage,
  } = useCart();
  const navigate = useNavigate();
  const { isAuthenticated, isAdmin } = useAuth();
  const { isAuthenticated } = useAuth();
  if (!isCartOpen && !successMessage) return null;

  return (
    <>
      {/* 🟢 Başarı Bildirimi (Toast) */}
      {successMessage && !isCartOpen && (
        <div
          style={{
            position: "fixed",
            top: "20px",
            right: "20px",
            zIndex: 1000,
            background: "#10b981",
            color: "white",
            padding: "16px 24px",
            borderRadius: "8px",
            boxShadow: "0 4px 6px rgba(0,0,0,0.1)",
            fontWeight: "500",
            display: "flex",
            alignItems: "center",
            gap: "8px",
          }}
        >
          <ShoppingBag size={20} /> {successMessage}
        </div>
      )}

      {/* 🟢 Sepet Çekmecesi (Drawer) */}
      {isCartOpen && (
        <>
          {/* Arka Plan Karartma */}
          <div
            onClick={() => setIsCartOpen(false)}
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              width: "100%",
              height: "100%",
              background: "rgba(0,0,0,0.5)",
              zIndex: 998,
            }}
          />
          {/* Çekmece */}
          <div
            style={{
              position: "fixed",
              top: 0,
              right: 0,
              width: "100%",
              maxWidth: "400px",
              height: "100%",
              background: "#fff",
              zIndex: 999,
              boxShadow: "-4px 0 15px rgba(0,0,0,0.1)",
              display: "flex",
              flexDirection: "column",
            }}
          >
            <div
              style={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                padding: "20px",
                borderBottom: "1px solid #e5e7eb",
              }}
            >
              <h2
                style={{
                  margin: 0,
                  fontSize: "20px",
                  display: "flex",
                  alignItems: "center",
                  gap: "8px",
                }}
              >
                <ShoppingBag size={24} /> Sepetim ({cart.length})
              </h2>
              <button
                onClick={() => setIsCartOpen(false)}
                style={{
                  background: "transparent",
                  border: "none",
                  cursor: "pointer",
                  color: "#6b7280",
                }}
              >
                <X size={24} />
              </button>
            </div>

            <div style={{ flex: 1, overflowY: "auto", padding: "20px" }}>
              {cart.length === 0 ? (
                <p
                  style={{
                    textAlign: "center",
                    color: "#6b7280",
                    marginTop: "40px",
                  }}
                >
                  Sepetiniz şu an boş.
                </p>
              ) : (
                cart.map((item) => (
                  <div
                    key={item.id}
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "16px",
                      marginBottom: "20px",
                      paddingBottom: "20px",
                      borderBottom: "1px solid #f3f4f6",
                    }}
                  >
                    {item.imageUrl ? (
                      <img
                        src={item.imageUrl}
                        alt={item.name}
                        style={{
                          width: "64px",
                          height: "64px",
                          objectFit: "cover",
                          borderRadius: "8px",
                        }}
                      />
                    ) : (
                      <div
                        style={{
                          width: "64px",
                          height: "64px",
                          background: "#f3f4f6",
                          borderRadius: "8px",
                        }}
                      />
                    )}
                    <div style={{ flex: 1 }}>
                      <h4 style={{ margin: "0 0 4px 0", fontSize: "15px" }}>
                        {item.name}
                      </h4>
                      <p
                        style={{
                          margin: 0,
                          color: "#6b7280",
                          fontSize: "14px",
                        }}
                      >
                        {item.quantity} x {item.price} TL
                      </p>
                    </div>
                    <button
                      onClick={() => removeFromCart(item.id)}
                      style={{
                        background: "#fee2e2",
                        border: "none",
                        padding: "8px",
                        borderRadius: "6px",
                        color: "#ef4444",
                        cursor: "pointer",
                      }}
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>
                ))
              )}
            </div>

            {cart.length > 0 && (
              <div
                style={{
                  padding: "20px",
                  borderTop: "1px solid #e5e7eb",
                  background: "#f9fafb",
                }}
              >
                <div
                  style={{
                    display: "flex",
                    justifyContent: "space-between",
                    marginBottom: "16px",
                    fontSize: "18px",
                    fontWeight: "bold",
                  }}
                >
                  <span>Ara Toplam:</span>
                  <span>{totalAmount} TL</span>
                </div>
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
                </button>
              </div>
            )}
          </div>
        </>
      )}
    </>
  );
}
