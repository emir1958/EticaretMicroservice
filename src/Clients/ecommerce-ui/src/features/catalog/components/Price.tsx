import { formatPrice } from "../../../shared/utils/format";
import styles from "./Price.module.css";

interface PriceProps {
  price: number;
  compareAtPrice?: number | null;
  size?: "md" | "lg";
  muted?: boolean;
}

// Satış fiyatı ve varsa üstü çizili liste fiyatı. Ekran okuyucu ikisini karıştırmasın diye etiketlenir.
export default function Price({ price, compareAtPrice, size = "md", muted = false }: PriceProps) {
  const hasDiscount = Boolean(compareAtPrice && compareAtPrice > price);

  return (
    <span className={`${styles.price} ${styles[size]} ${muted ? styles.muted : ""}`}>
      <span className={hasDiscount ? styles.sale : undefined}>
        {hasDiscount && <span className="visually-hidden">İndirimli fiyat: </span>}
        {formatPrice(price)}
      </span>
      {hasDiscount && (
        <s className={styles.compare}>
          <span className="visually-hidden">Önceki fiyat: </span>
          {formatPrice(compareAtPrice)}
        </s>
      )}
    </span>
  );
}
