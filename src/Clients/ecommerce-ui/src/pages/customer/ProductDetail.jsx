import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { ArrowLeft, Package, Minus, Plus, ShoppingCart } from "lucide-react";
import api from "../../services/api";
import { useCart } from "../../context/CartContext";
import { useAuth } from "../../context/AuthContext";
import ProductCard from "../../components/ProductCard";
import Button from "../../components/ui/Button";
import { formatPrice } from "../../utils/orderStatus";
import styles from "./ProductDetail.module.css";

// Başka bir ürüne geçildiğinde (ilginizi çekebilir) sayfa id ile key'lenir: React component'i yeniden oluşturur,
// adet ve yükleme durumu gibi tüm state kendiliğinden sıfırlanır.
export default function ProductDetail() {
  const { id } = useParams();
  return <ProductDetailContent key={id} id={id} />;
}

function ProductDetailContent({ id }) {
  const { isAdmin } = useAuth();
  const [product, setProduct] = useState(null);
  const [stockCount, setStockCount] = useState(0);
  const [otherProducts, setOtherProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [quantity, setQuantity] = useState(1);
  const { addToCart } = useCart();

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [prodRes, stockRes, allProdsRes] = await Promise.all([
          api.get(`/api/Products/${id}`),
          api.get(`/api/stocks/${id}`).catch((err) => {
            console.warn("Stok servisi yanıt vermedi:", err);
            return { data: null };
          }),
          api.get(`/api/Products`),
        ]);

        setProduct(prodRes.data?.data || prodRes.data);

        // 🟢 availableStock veya AvailableStock kontrolü
        const stockData = stockRes.data?.data || stockRes.data;
        const count =
          stockData?.availableStock ?? stockData?.AvailableStock ?? 0;
        setStockCount(count);

        const allList = Array.isArray(allProdsRes.data)
          ? allProdsRes.data
          : allProdsRes.data?.data || [];

        setOtherProducts(allList.filter((p) => p.id !== id).slice(0, 4));
      } catch (err) {
        console.error("Veriler alınamadı", err);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [id]);

  if (loading) return <div className={styles.state}>Yükleniyor...</div>;
  if (!product) return <div className={styles.state}>Ürün bulunamadı.</div>;

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
