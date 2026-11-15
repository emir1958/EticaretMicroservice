import { useState } from "react";
import { useAsync } from "../../../shared/hooks/useAsync";
import { useParams, Link } from "react-router-dom";
import { ArrowLeft, Package, ShoppingCart } from "lucide-react";
import { getProduct, getProducts } from "../api";
import { getAvailableStock } from "../../stock/api";
import { useCart } from "../../cart/CartContext";
import { useAuth } from "../../auth/AuthContext";
import ProductCard from "../components/ProductCard";
import Button from "../../../shared/components/Button";
import QuantityStepper from "../../../shared/components/QuantityStepper";
import { formatPrice } from "../../../shared/utils/format";
import styles from "./ProductDetailPage.module.css";

// Bu ekranın ihtiyacı üç isteğin birleşimi; api.ts fonksiyonlarını bir araya getirir, HTTP bilmez.
async function loadProductDetail(id: string) {
  const [product, stockCount, allProducts] = await Promise.all([
    getProduct(id),
    // Stok servisi yanıt vermezse ürün yine gösterilir, stokta yok olarak
    getAvailableStock(id).catch((err) => {
      console.warn("Stok servisi yanıt vermedi:", err);
      return 0;
    }),
    getProducts(),
  ]);
  return {
    product,
    stockCount,
    otherProducts: allProducts.filter((p) => p.id !== id).slice(0, 4),
  };
}

// Başka bir ürüne geçildiğinde (ilginizi çekebilir) sayfa id ile key'lenir: React component'i yeniden oluşturur,
// adet gibi kullanıcının girdiği state kendiliğinden sıfırlanır.
export default function ProductDetailPage() {
  const { id } = useParams();
  return id ? <ProductDetailContent key={id} id={id} /> : null;
}

function ProductDetailContent({ id }: { id: string }) {
  const { isAdmin } = useAuth();
  const { addToCart, getCartQuantity } = useCart();
  const [quantity, setQuantity] = useState(1);
  const { data, isLoading } = useAsync(() => loadProductDetail(id), [id]);

  if (isLoading) return <div className={styles.state}>Yükleniyor...</div>;
  if (!data) return <div className={styles.state}>Ürün bulunamadı.</div>;

  const { product, stockCount, otherProducts } = data;

  const isOutOfStock = stockCount <= 0;
  const remaining = stockCount - getCartQuantity(product.id);
  const isAllInCart = !isOutOfStock && remaining <= 0;
  const canAdd = !isAdmin && remaining > 0;

  const handleAdd = () => {
    addToCart(product, quantity, stockCount);
    setQuantity(1);
  };

  return (
    <>
      <Link to="/" className={styles.back}>
        <ArrowLeft size={16} /> Mağazaya dön
      </Link>

      <section className={styles.product}>
        <div className={`${styles.media} ${isOutOfStock ? styles.outOfStock : ""}`}>
          {isOutOfStock && <span className={styles.soldOut}>TÜKENDİ</span>}
          {product.imageUrl ? <img src={product.imageUrl} alt={product.name} /> : <Package size={80} />}
        </div>

        <div className={styles.info}>
          <h1 className={styles.name}>{product.name}</h1>
          <p className={styles.description}>
            {product.description || "Bu ürün için henüz bir açıklama girilmemiştir."}
          </p>
          <div className={styles.price}>{formatPrice(product.price)}</div>
          <span className={`${styles.stock} ${isOutOfStock ? styles.noStock : styles.inStock}`}>
            {isOutOfStock ? "Stokta yok" : `Stokta var · ${stockCount} adet`}
          </span>

          <div className={styles.divider} />

          <div className={styles.purchase}>
            <QuantityStepper value={quantity} max={Math.max(remaining, 1)} onChange={setQuantity} />

            <Button
              size="lg"
              className={styles.addButton}
              variant={canAdd ? "primary" : "secondary"}
              onClick={handleAdd}
              disabled={!canAdd}
            >
              <ShoppingCart size={18} />
              {isAdmin
                ? "Yönetici hesabı ile sipariş verilemez"
                : isOutOfStock
                  ? "Tükendi"
                  : isAllInCart
                    ? "Stoktaki tüm adetler sepetinizde"
                    : "Sepete Ekle"}
            </Button>
          </div>
        </div>
      </section>

      {otherProducts.length > 0 && (
        <section className={styles.related}>
          <h2 className={styles.sectionTitle}>Bunlar da ilginizi çekebilir</h2>
          <div className={styles.relatedGrid}>
            {otherProducts.map((op) => (
              <ProductCard key={op.id} product={op} showDescription={false} />
            ))}
          </div>
        </section>
      )}
    </>
  );
}
