import { useNavigate } from "react-router-dom";
import { X, Trash2, ShoppingBag, CheckCircle } from "lucide-react";
import { useCart } from "../context/CartContext";
import { useAuth } from "../context/AuthContext";
import Button from "./ui/Button";
import EmptyState from "./ui/EmptyState";
import { formatPrice } from "../utils/orderStatus";
import styles from "./CartDrawer.module.css";

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
  if (!isCartOpen && !successMessage) return null;

  return (
    <>
      {/* 🟢 Başarı Bildirimi (Toast) */}
      {successMessage && !isCartOpen && (
        <div className={styles.toast} role="status">
          <CheckCircle size={18} className={styles.toastIcon} /> {successMessage}
        </div>
      )}

      {/* 🟢 Sepet Çekmecesi (Drawer) */}
      {isCartOpen && (
        <>
          <div className={styles.overlay} onClick={() => setIsCartOpen(false)} />
          <aside className={styles.drawer} aria-label="Sepetim">
            <div className={styles.header}>
              <h2 className={styles.title}>
                <ShoppingBag size={22} /> Sepetim
                {cart.length > 0 && <span className={styles.countPill}>{cart.length}</span>}
              </h2>
              <button type="button" className={styles.close} onClick={() => setIsCartOpen(false)} aria-label="Kapat">
                <X size={20} />
              </button>
            </div>

            <div className={styles.body}>
              {cart.length === 0 ? (
                <div className={styles.emptyWrap}>
                  <EmptyState
                    icon={ShoppingBag}
                    title="Sepetiniz boş"
                    text="Beğendiğiniz ürünleri sepete ekleyin, burada görünsün."
                  />
                </div>
              ) : (
                cart.map((item) => (
                  <div key={item.id} className={styles.item}>
                    {item.imageUrl ? (
                      <img src={item.imageUrl} alt={item.name} className={styles.thumb} />
                    ) : (
                      <div className={styles.thumb} />
                    )}
                    <div className={styles.itemInfo}>
                      <div className={styles.itemName}>{item.name}</div>
                      <div className={styles.itemMeta}>
                        {item.quantity} × {formatPrice(item.price)}
                      </div>
                    </div>
                    <button
                      type="button"
                      className={styles.remove}
                      onClick={() => removeFromCart(item.id)}
                      aria-label={`${item.name} ürününü sepetten çıkar`}
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>
                ))
              )}
            </div>

            {cart.length > 0 && (
              <div className={styles.footer}>
                <div className={styles.total}>
                  <span>Ara toplam</span>
                  <span>{formatPrice(totalAmount)}</span>
                </div>
                {isAdmin ? (
                  <div className={styles.adminNote}>Yönetici hesapları ile sipariş oluşturulamaz.</div>
                ) : (
                  <Button
                    size="lg"
                    fullWidth
                    onClick={() => {
                      setIsCartOpen(false);
                      if (!isAuthenticated) {
                        navigate("/auth?redirect=/checkout");
                      } else {
                        navigate("/checkout");
                      }
                    }}
                  >
                    Siparişi Tamamla
                  </Button>
                )}
              </div>
            )}
          </aside>
        </>
      )}
    </>
  );
}
