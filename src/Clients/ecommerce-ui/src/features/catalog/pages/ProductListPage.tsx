import { useAsync } from "../../../shared/hooks/useAsync";
import { Package } from "lucide-react";
import { getProductsWithStock } from "../api";
import { useCart } from "../../cart/CartContext";
import { useAuth } from "../../auth/AuthContext";
import ProductCard from "../components/ProductCard";
import Button from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import styles from "./ProductListPage.module.css";

export default function ProductListPage() {
  const { data: products = [], error, isLoading } = useAsync(getProductsWithStock, []);
  const { addToCart } = useCart();
  const { isAdmin } = useAuth();

  return (
    <>
      <section className={styles.hero}>
        <div>
          <h1 className={styles.heroTitle}>Keşfetmeye başla</h1>
          <p className={styles.heroText}>Stoktaki ürünleri incele, sepetine ekle ve siparişini saniyeler içinde ver.</p>
        </div>
        {!isLoading && !error && <span className={styles.count}>{products.length} ürün</span>}
      </section>

      {error && (
        <div className={styles.error}>
          Ürünler yüklenirken bir hata oluştu. Gateway veya Catalog servisi çalışıyor mu?
        </div>
      )}

      {isLoading ? (
        <div className={styles.grid}>
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className={styles.skeleton} />
          ))}
        </div>
      ) : !error && products.length === 0 ? (
        <EmptyState icon={Package} title="Henüz ürün yok" text="Mağazaya ürün eklendiğinde burada görünecek." />
      ) : (
        <div className={styles.grid}>
          {products.map((p) => {
            const isOutOfStock = p.availableStock <= 0;
            return (
              <ProductCard
                key={p.id}
                product={p}
                isOutOfStock={isOutOfStock}
                action={
                  <Button
                    size="sm"
                    variant={isAdmin || isOutOfStock ? "secondary" : "primary"}
                    disabled={isAdmin || isOutOfStock}
                    onClick={() => {
                      if (isAdmin || isOutOfStock) return;
                      addToCart({ id: p.id, name: p.name, price: p.price, imageUrl: p.imageUrl });
                    }}
                  >
                    {isAdmin ? "Yönetici Modu" : isOutOfStock ? "Tükendi" : "Sepete Ekle"}
                  </Button>
                }
              />
            );
          })}
        </div>
      )}
    </>
  );
}
