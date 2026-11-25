import { Link, useNavigate } from "react-router-dom";
import { ShoppingBag, Trash2, Truck } from "lucide-react";
import { useCart } from "../CartContext";
import { useCartStock } from "../useCartStock";
import { useAuth } from "../../auth/AuthContext";
import Alert from "../../../shared/components/Alert";
import Badge from "../../../shared/components/Badge";
import Button from "../../../shared/components/Button";
import Dialog from "../../../shared/components/Dialog";
import EmptyState from "../../../shared/components/EmptyState";
import IconButton from "../../../shared/components/IconButton";
import Picture from "../../../shared/components/Picture";
import QuantityStepper from "../../../shared/components/QuantityStepper";
import { formatPrice } from "../../../shared/utils/format";
import styles from "./CartDrawer.module.css";

export default function CartDrawer() {
  const { isCartOpen, setIsCartOpen, cart, removeFromCart, updateQuantity, totalAmount } = useCart();
  const navigate = useNavigate();
  const { isAuthenticated, isAdmin } = useAuth();
  const { stockFor, hasStockIssue } = useCartStock(isCartOpen);

  const close = () => setIsCartOpen(false);
  const itemCount = cart.reduce((sum, item) => sum + item.quantity, 0);

  const goToCheckout = () => {
    close();
    navigate(isAuthenticated ? "/checkout" : "/auth?redirect=/checkout");
  };

  return (
    <Dialog
      open={isCartOpen}
      onClose={close}
      title="Sepetim"
      titleExtra={itemCount > 0 && <Badge>{itemCount} ürün</Badge>}
      footer={
        cart.length > 0 && (
          <div className={styles.summary}>
            <div className={styles.row}>
              <span>Kargo</span>
              <span className={styles.free}>
                <Truck size={16} aria-hidden="true" /> Ücretsiz
              </span>
            </div>
            <div className={`${styles.row} ${styles.total}`}>
              <span>Toplam</span>
              <span>{formatPrice(totalAmount)}</span>
            </div>
            {isAdmin ? (
              <Alert tone="info">Yönetici hesaplarıyla sipariş oluşturulamaz.</Alert>
            ) : (
              <>
                {hasStockIssue && <Alert>Stoğu aşan ürünlerin adedini düzeltin.</Alert>}
                <Button size="lg" fullWidth disabled={hasStockIssue} onClick={goToCheckout}>
                  {isAuthenticated ? "Siparişi tamamla" : "Giriş yap ve siparişi tamamla"}
                </Button>
              </>
            )}
          </div>
        )
      }
    >
      {cart.length === 0 ? (
        <EmptyState
          variant="plain"
          icon={ShoppingBag}
          title="Sepetin boş"
          text="Beğendiğin ürünleri sepete ekle, burada görünsün."
          action={
            <Button variant="secondary" onClick={close}>
              Alışverişe devam et
            </Button>
          }
        />
      ) : (
        <ul className={styles.items}>
          {cart.map((item) => {
            const stock = stockFor(item.id);
            const exceedsStock = stock !== undefined && item.quantity > stock;
            return (
              <li key={item.id} className={styles.item}>
                <Link to={`/product/${item.id}`} onClick={close}>
                  <Picture src={item.imageUrl} alt={item.name} width={88} className={styles.thumb} />
                </Link>
                <div className={styles.itemInfo}>
                  <Link to={`/product/${item.id}`} onClick={close} className={styles.itemName}>
                    {item.name}
                  </Link>
                  <span className={styles.itemUnit}>{formatPrice(item.price)}</span>
                  <div className={styles.itemControls}>
                    <QuantityStepper
                      size="sm"
                      label={item.name}
                      value={item.quantity}
                      max={stock}
                      onChange={(quantity) => updateQuantity(item.id, quantity)}
                    />
                    <span className={styles.itemTotal}>{formatPrice(item.price * item.quantity)}</span>
                  </div>
                  {exceedsStock && (
                    <span className={styles.stockWarning}>
                      {stock === 0 ? "Bu ürün stokta kalmadı" : `Stokta yalnızca ${stock} adet var`}
                    </span>
                  )}
                </div>
                <IconButton
                  icon={Trash2}
                  size="sm"
                  label={`${item.name} ürününü sepetten çıkar`}
                  onClick={() => removeFromCart(item.id)}
                />
              </li>
            );
          })}
        </ul>
      )}
    </Dialog>
  );
}
