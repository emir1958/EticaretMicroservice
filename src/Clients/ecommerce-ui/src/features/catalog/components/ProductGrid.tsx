import Skeleton from "../../../shared/components/Skeleton";
import type { ProductWithStock } from "../types";
import ProductCard from "./ProductCard";
import styles from "./ProductGrid.module.css";

interface ProductGridProps {
  products: ProductWithStock[];
  // true iken ürünler yerine aynı yerleşimde yer tutucular gösterilir (sayfa yüklenince zıplamaz)
  isLoading?: boolean;
  skeletonCount?: number;
  // wide: yanında filtre paneli olmayan tam genişlik ızgara (4 sütun)
  columns?: "default" | "wide";
}

export default function ProductGrid({ products, isLoading = false, skeletonCount = 8, columns = "default" }: ProductGridProps) {
  return (
    <div className={`${styles.grid} ${styles[columns]}`} aria-busy={isLoading}>
      {isLoading
        ? Array.from({ length: skeletonCount }, (_, i) => (
            <div key={i}>
              <Skeleton className={styles.skeletonMedia} />
              <Skeleton className={styles.skeletonLine} />
              <Skeleton className={styles.skeletonLineShort} />
            </div>
          ))
        : products.map((product) => <ProductCard key={product.id} product={product} />)}
    </div>
  );
}
