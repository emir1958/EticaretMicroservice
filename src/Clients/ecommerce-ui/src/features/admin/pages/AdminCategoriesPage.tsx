import { Link } from "react-router-dom";
import { ArrowUpRight, Tags } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import Alert from "../../../shared/components/Alert";
import Badge from "../../../shared/components/Badge";
import Button from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import Picture from "../../../shared/components/Picture";
import Skeleton from "../../../shared/components/Skeleton";
import { formatPrice } from "../../../shared/utils/format";
import { getProductsWithStock } from "../../catalog/api";
import { categoryOf, listCategories, UNCATEGORIZED } from "../../catalog/catalogView";
import { getProductSales } from "../../orders/api";
import { periodsFor } from "../analytics";
import ui from "../admin.module.css";
import styles from "./AdminCategoriesPage.module.css";

async function loadCategories() {
  // Gelir son 30 günün çok satanlar raporundan (Order servisi SQL ile hesaplar); servis yanıt vermezse sayfa yine açılır
  const [products, sales] = await Promise.all([getProductsWithStock(), getProductSales(periodsFor(30).current).catch(() => [])]);
  const revenueByProduct = new Map(sales.map((sale) => [sale.productId, sale.revenue]));

  return listCategories(products).map((category) => {
    const own = products.filter((p) => categoryOf(p) === category.name);
    return {
      ...category,
      totalStock: own.reduce((sum, p) => sum + p.availableStock, 0),
      outOfStock: own.filter((p) => p.availableStock <= 0).length,
      revenue: own.reduce((sum, p) => sum + (revenueByProduct.get(p.id) ?? 0), 0),
    };
  });
}

export default function AdminCategoriesPage() {
  const { data: categories = [], error, isLoading, reload } = useAsync(loadCategories, []);

  return (
    <>
      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>Kategoriler</h1>
          <p className={ui.subtitle}>Kategoriler ürünlerden türetilir; satış rakamları son 30 günü kapsar.</p>
        </div>
      </div>

      <Alert tone="info" className={ui.alert}>
        Yeni kategori açmak için bir ürünün kategori alanına yeni bir ad yazmanız yeterli. İçinde ürün kalmayan kategori
        mağazadan kendiliğinden kalkar.
      </Alert>

      {isLoading ? (
        <div className={styles.grid} aria-busy="true" aria-label="Kategoriler yükleniyor">
          {[1, 2, 3, 4].map((i) => (
            <Skeleton key={i} className={styles.skeleton} />
          ))}
        </div>
      ) : error ? (
        <EmptyState
          icon={Tags}
          title="Kategoriler yüklenemedi"
          text={getErrorMessage(error, "Catalog servisine ulaşılamıyor.")}
          action={<Button onClick={reload}>Tekrar dene</Button>}
        />
      ) : categories.length === 0 ? (
        <EmptyState icon={Tags} title="Henüz kategori yok" text="Ürün eklediğinizde kategoriler burada listelenir." />
      ) : (
        <div className={styles.grid}>
          {categories.map((category) => (
            <article key={category.name} className={styles.card}>
              <Picture src={category.imageUrl} alt="" width={420} className={styles.picture} />
              <div className={styles.body}>
                <div className={styles.titleRow}>
                  <h2 className={styles.name}>{category.name}</h2>
                  {category.name === UNCATEGORIZED && <Badge tone="warning">Kategorisiz</Badge>}
                </div>
                <dl className={styles.metrics}>
                  <div>
                    <dt>Ürün</dt>
                    <dd>{category.count}</dd>
                  </div>
                  <div>
                    <dt>Stok</dt>
                    <dd>{category.totalStock} adet</dd>
                  </div>
                  <div>
                    <dt>30 gün ciro</dt>
                    <dd>{formatPrice(category.revenue)}</dd>
                  </div>
                </dl>
                {category.outOfStock > 0 && (
                  <p className={styles.warning}>{category.outOfStock} ürün tükendi</p>
                )}
                <div className={styles.links}>
                  <Link to={`/admin/products?category=${encodeURIComponent(category.name)}`} className={styles.link}>
                    Ürünleri yönet
                  </Link>
                  {category.name !== UNCATEGORIZED && (
                    <Link
                      to={`/products?category=${encodeURIComponent(category.name)}`}
                      target="_blank"
                      className={styles.linkMuted}
                    >
                      Mağazada gör <ArrowUpRight size={14} aria-hidden="true" />
                    </Link>
                  )}
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </>
  );
}
