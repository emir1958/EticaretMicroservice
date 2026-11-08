import { Link } from "react-router-dom";
import { Package } from "lucide-react";
import { formatPrice } from "../../../shared/utils/format";
import styles from "./ProductCard.module.css";

// Ürün listesi ve "ilginizi çekebilir" bölümünde ortak kart. action: fiyatın yanındaki buton (opsiyonel).
export default function ProductCard({ product, isOutOfStock = false, showDescription = true, action }) {
  return (
    <article className={`${styles.card} ${isOutOfStock ? styles.outOfStock : ""}`}>
      <Link to={`/product/${product.id}`}>
        <div className={styles.media}>
          {isOutOfStock && <span className={styles.soldOut}>TÜKENDİ</span>}
          {product.imageUrl ? <img src={product.imageUrl} alt={product.name} loading="lazy" /> : <Package size={48} />}
        </div>
        <div className={styles.body}>
          <h3 className={styles.name}>{product.name}</h3>
          {showDescription && (
            <p className={styles.description}>{product.description || "Açıklama bulunmuyor"}</p>
          )}
        </div>
      </Link>
      <div className={styles.footer}>
        <span className={`${styles.price} ${isOutOfStock ? styles.priceMuted : ""}`}>{formatPrice(product.price)}</span>
        {action}
      </div>
    </article>
  );
}
