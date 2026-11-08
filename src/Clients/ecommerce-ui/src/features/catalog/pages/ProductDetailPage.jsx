import { useState } from "react";
import { useAsync } from "../../../shared/hooks/useAsync";
import { useParams, Link } from "react-router-dom";
import { ArrowLeft, Package, Minus, Plus, ShoppingCart } from "lucide-react";
import { getAvailableStock, getProduct, getProducts } from "../api";
import { useCart } from "../../cart/CartContext";
import { useAuth } from "../../auth/AuthContext";
import ProductCard from "../components/ProductCard";
import Button from "../../../shared/components/Button";
import { formatPrice } from "../../../shared/utils/format";
import styles from "./ProductDetailPage.module.css";

// Bu ekranın ihtiyacı üç isteğin birleşimi; api.js fonksiyonlarını bir araya getirir, HTTP bilmez.
async function loadProductDetail(id) {
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
  return <ProductDetailContent key={id} id={id} />;
}

function ProductDetailContent({ id }) {
  const { isAdmin } = useAuth();
  const { addToCart } = useCart();
  const [quantity, setQuantity] = useState(1);
  const { data, isLoading } = useAsync(() => loadProductDetail(id), [id]);

  if (isLoading) return <div className={styles.state}>Yükleniyor...</div>;
  if (!data) return <div className={styles.state}>Ürün bulunamadı.</div>;

  const { product, stockCount, otherProducts } = data;

  const isOutOfStock = stockCount <= 0;
  const handleAdd = () => {
    if (quantity < stockCount) setQuantity((q) => q + 1);
  };
  const handleSub = () => {
    if (quantity > 1) setQuantity((q) => q - 1);
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
            <div className={styles.stepper}>
              <button type="button" onClick={handleSub} disabled={quantity <= 1} aria-label="Adedi azalt">
                <Minus size={16} />
              </button>
              <span className={styles.quantity}>{quantity}</span>
              <button type="button" onClick={handleAdd} disabled={quantity >= stockCount} aria-label="Adedi artır">
                <Plus size={16} />
              </button>
            </div>

            <Button
              size="lg"
              className={styles.addButton}
              variant={isAdmin || isOutOfStock ? "secondary" : "primary"}
              onClick={() => !isAdmin && addToCart({ ...product, id: product.id }, quantity)}
              disabled={stockCount === 0 || isAdmin}
            >
              <ShoppingCart size={18} />
              {isAdmin
                ? "Yönetici hesabı ile sipariş verilemez"
                : stockCount === 0
                  ? "Tükendi"
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
