import { Link } from "react-router-dom";
import { Check, Plus } from "lucide-react";
import { useAuth } from "../../auth/AuthContext";
import { useCart } from "../../cart/CartContext";
import Badge from "../../../shared/components/Badge";
import Picture from "../../../shared/components/Picture";
import { categoryOf, discountPercent, stockStateOf } from "../catalogView";
import type { ProductWithStock } from "../types";
import Price from "./Price";
import styles from "./ProductCard.module.css";

// Mağazadaki tüm ürün ızgaralarının kartı: görsel + ad bağlantıdır, hızlı ekleme butonu bağlantının dışındadır
// (bağlantı içinde buton olmaz).
export default function ProductCard({ product }: { product: ProductWithStock }) {
  const { addToCart, getCartQuantity } = useCart();
  const { isAdmin } = useAuth();

  const stockState = stockStateOf(product.availableStock);
  const discount = discountPercent(product);
  const isAllInCart = stockState !== "out" && getCartQuantity(product.id) >= product.availableStock;
  const canAdd = !isAdmin && stockState !== "out" && !isAllInCart;

  return (
    <article className={`${styles.card} ${stockState === "out" ? styles.outOfStock : ""}`}>
      <Link to={`/product/${product.id}`} className={styles.link}>
        <div className={styles.media}>
          <Picture src={product.imageUrl} alt={product.name} width={420} className={styles.picture} />
          <div className={styles.badges}>
            {stockState === "out" ? (
              <Badge tone="solid">Tükendi</Badge>
            ) : (
              <>
                {discount !== null && <Badge tone="accent">%{discount} indirim</Badge>}
                {stockState === "low" && <Badge tone="warning">Son {product.availableStock} adet</Badge>}
              </>
            )}
          </div>
        </div>
        <div className={styles.body}>
          <span className={styles.category}>{categoryOf(product)}</span>
          <h3 className={styles.name}>{product.name}</h3>
          <Price price={product.price} compareAtPrice={product.compareAtPrice} muted={stockState === "out"} />
        </div>
      </Link>

      {!isAdmin && stockState !== "out" && (
        <button
          type="button"
          className={styles.quickAdd}
          disabled={!canAdd}
          onClick={() => addToCart(product, 1, product.availableStock)}
          aria-label={isAllInCart ? `${product.name}: stoktaki tüm adetler sepetinizde` : `${product.name} ürününü sepete ekle`}
        >
          {isAllInCart ? <Check size={18} aria-hidden="true" /> : <Plus size={18} aria-hidden="true" />}
        </button>
      )}
    </article>
  );
}
