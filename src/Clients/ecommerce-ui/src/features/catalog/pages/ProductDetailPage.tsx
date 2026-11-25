import { useState } from "react";
import { useParams } from "react-router-dom";
import { PackageCheck, PackageX, Radio, ShoppingBag, Truck } from "lucide-react";
import { getErrorStatus } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import Badge from "../../../shared/components/Badge";
import Breadcrumb from "../../../shared/components/Breadcrumb";
import Button, { ButtonLink } from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import Picture from "../../../shared/components/Picture";
import QuantityStepper from "../../../shared/components/QuantityStepper";
import Skeleton from "../../../shared/components/Skeleton";
import { useAuth } from "../../auth/AuthContext";
import { useCart } from "../../cart/CartContext";
import { getAvailableStock } from "../../stock/api";
import { getProduct, getProductsWithStock } from "../api";
import { categoryOf, discountPercent, sortProducts, stockStateOf } from "../catalogView";
import Price from "../components/Price";
import ProductGrid from "../components/ProductGrid";
import styles from "./ProductDetailPage.module.css";

const STOCK_VIEW = {
  in: { tone: "success", label: "Stokta" },
  low: { tone: "warning", label: "Tükenmek üzere" },
  out: { tone: "neutral", label: "Stokta yok" },
} as const;

// Bu ekranın ihtiyacı üç isteğin birleşimi; api.ts fonksiyonlarını bir araya getirir, HTTP bilmez.
async function loadProductDetail(id: string) {
  const [product, stockCount, allProducts] = await Promise.all([
    getProduct(id),
    // Stok servisi yanıt vermezse ürün yine gösterilir, stokta yok olarak
    getAvailableStock(id).catch((err) => {
      console.warn("Stok servisi yanıt vermedi:", err);
      return 0;
    }),
    // Benzer ürünler sayfanın asıl içeriği değil: alınamazsa bölüm gösterilmez
    getProductsWithStock().catch(() => []),
  ]);

  const others = sortProducts(
    allProducts.filter((p) => p.id !== id),
    "featured",
  );
  const sameCategory = others.filter((p) => categoryOf(p) === categoryOf(product));

  return {
    product,
    stockCount,
    related: (sameCategory.length >= 2 ? sameCategory : others).slice(0, 4),
  };
}

// Başka bir ürüne geçildiğinde sayfa id ile key'lenir: React component'i yeniden oluşturur,
// adet gibi kullanıcının girdiği state kendiliğinden sıfırlanır.
export default function ProductDetailPage() {
  const { id } = useParams();
  return id ? <ProductDetailContent key={id} id={id} /> : null;
}

function ProductDetailContent({ id }: { id: string }) {
  const { isAdmin } = useAuth();
  const { addToCart, getCartQuantity } = useCart();
  const [quantity, setQuantity] = useState(1);
  const { data, error, isLoading, reload } = useAsync(() => loadProductDetail(id), [id]);

  if (isLoading) return <DetailSkeleton />;

  if (!data) {
    const isNotFound = getErrorStatus(error) === 404 || getErrorStatus(error) === 400;
    return (
      <EmptyState
        icon={PackageX}
        title={isNotFound ? "Bu ürünü bulamadık" : "Ürün şu anda yüklenemiyor"}
        text={
          isNotFound
            ? "Ürün kaldırılmış veya bağlantı hatalı olabilir."
            : "Katalog servisine ulaşılamadı. Birkaç saniye sonra tekrar deneyin."
        }
        action={
          <>
            {!isNotFound && <Button onClick={reload}>Tekrar dene</Button>}
            <ButtonLink to="/products" variant={isNotFound ? "primary" : "secondary"}>
              Tüm ürünlere dön
            </ButtonLink>
          </>
        }
      />
    );
  }

  const { product, stockCount, related } = data;
  const category = categoryOf(product);
  const stockState = stockStateOf(stockCount);
  const stockView = STOCK_VIEW[stockState];
  const discount = discountPercent(product);

  const inCart = getCartQuantity(product.id);
  const remaining = stockCount - inCart;
  const isAllInCart = stockState !== "out" && remaining <= 0;
  const canAdd = !isAdmin && remaining > 0;

  const handleAdd = () => {
    addToCart(product, quantity, stockCount);
    setQuantity(1);
  };

  const addLabel = isAdmin
    ? "Yönetici hesabıyla sipariş verilemez"
    : stockState === "out"
      ? "Tükendi"
      : isAllInCart
        ? "Stoktaki tüm adetler sepetinde"
        : "Sepete ekle";

  return (
    <>
      <Breadcrumb
        items={[
          { label: "Ana sayfa", to: "/" },
          { label: category, to: `/products?category=${encodeURIComponent(category)}` },
          { label: product.name },
        ]}
      />

      <section className={styles.product}>
        <div className={styles.media}>
          <Picture
            src={product.imageUrl}
            alt={product.name}
            width={720}
            loading="eager"
            className={`${styles.picture} ${stockState === "out" ? styles.pictureOut : ""}`}
          />
          {discount !== null && stockState !== "out" && (
            <Badge tone="accent" className={styles.mediaBadge}>
              %{discount} indirim
            </Badge>
          )}
        </div>

        <div className={styles.info}>
          <span className={styles.category}>{category}</span>
          <h1 className={styles.name}>{product.name}</h1>

          <div className={styles.priceRow}>
            <Price price={product.price} compareAtPrice={product.compareAtPrice} size="lg" />
          </div>

          <div className={styles.stock}>
            <Badge tone={stockView.tone} dot>
              {stockView.label}
            </Badge>
            {stockState === "low" && <span>Son {stockCount} adet kaldı</span>}
            {inCart > 0 && <span>Sepetinde {inCart} adet var</span>}
          </div>

          <p className={styles.description}>
            {product.description || "Bu ürün için henüz bir açıklama girilmedi."}
          </p>

          <div className={styles.purchase}>
            <QuantityStepper value={quantity} max={Math.max(remaining, 1)} onChange={setQuantity} label={product.name} />
            <Button size="lg" className={styles.addButton} onClick={handleAdd} disabled={!canAdd}>
              <ShoppingBag size={18} aria-hidden="true" />
              {addLabel}
            </Button>
          </div>

          <ul className={styles.assurances}>
            <li>
              <Truck size={18} aria-hidden="true" /> Ücretsiz kargo
            </li>
            <li>
              <PackageCheck size={18} aria-hidden="true" /> Sipariş anında stok senin için ayrılır
            </li>
            <li>
              <Radio size={18} aria-hidden="true" /> Siparişini canlı takip edersin
            </li>
          </ul>
        </div>
      </section>

      {related.length > 0 && (
        <section className={styles.related}>
          <h2 className={styles.sectionTitle}>Bunlar da ilgini çekebilir</h2>
          <ProductGrid products={related} columns="wide" />
        </section>
      )}
    </>
  );
}

function DetailSkeleton() {
  return (
    <div aria-busy="true" aria-label="Ürün yükleniyor">
      <Skeleton className={styles.skeletonCrumb} />
      <div className={styles.product}>
        <Skeleton className={styles.skeletonMedia} />
        <div className={styles.info}>
          <Skeleton className={styles.skeletonLineShort} />
          <Skeleton className={styles.skeletonTitle} />
          <Skeleton className={styles.skeletonLineShort} />
          <Skeleton className={styles.skeletonBlock} />
          <Skeleton className={styles.skeletonButton} />
        </div>
      </div>
    </div>
  );
}
