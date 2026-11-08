import { useEffect, useState } from "react";
import { Package } from "lucide-react";
import api from "../../services/api";
import { useCart } from "../../context/CartContext";
import { useAuth } from "../../context/AuthContext";
import ProductCard from "../../components/ProductCard";
import Button from "../../components/ui/Button";
import EmptyState from "../../components/ui/EmptyState";
import styles from "./ProductList.module.css";

export default function ProductList() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const { addToCart } = useCart();
  const { isAdmin } = useAuth();

  useEffect(() => {
    // 🟢 Catalog ürünleri ile StockDb canlı stoklarını paralel çekip birleştiriyoruz
    Promise.all([
      api.get("/api/Products"),
      api.get("/api/stocks").catch((err) => {
        console.warn("Stok servisi yanıt vermedi:", err);
        return { data: [] };
      }),
    ])
      .then(([prodRes, stockRes]) => {
        const productList = Array.isArray(prodRes.data)
          ? prodRes.data
          : prodRes.data?.data || [];

        const stockList = Array.isArray(stockRes.data) ? stockRes.data : [];
        const stockMap = new Map(
          stockList.map((s) => [s.productId, s.availableStock]),
        );

        // Her ürüne güncel availableStock değerini ekliyoruz
        const mergedList = productList.map((p) => ({
          ...p,
          availableStock: stockMap.has(p.id) ? stockMap.get(p.id) : 0,
        }));

        setProducts(mergedList);
        setLoading(false);
      })
      .catch((err) => {
        console.error("Katalog çekme hatası:", err);
        setError(
          "Ürünler yüklenirken bir hata oluştu. Gateway veya Catalog servisi çalışıyor mu?",
        );
        setLoading(false);
      });
  }, []);

  return (
    <>
      <section className={styles.hero}>
        <div>
          <h1 className={styles.heroTitle}>Keşfetmeye başla</h1>
          <p className={styles.heroText}>Stoktaki ürünleri incele, sepetine ekle ve siparişini saniyeler içinde ver.</p>
        </div>
        {!loading && !error && <span className={styles.count}>{products.length} ürün</span>}
      </section>

      {error && <div className={styles.error}>{error}</div>}

      {loading ? (
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
