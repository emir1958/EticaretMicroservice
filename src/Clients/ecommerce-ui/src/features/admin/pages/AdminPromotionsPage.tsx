import { useState, type SubmitEvent } from "react";
import { BadgePercent, Percent, Tag } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import { useToast } from "../../../shared/toast/ToastContext";
import Alert from "../../../shared/components/Alert";
import Badge from "../../../shared/components/Badge";
import Button from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import FormField, { SelectField } from "../../../shared/components/FormField";
import Picture from "../../../shared/components/Picture";
import Skeleton from "../../../shared/components/Skeleton";
import { formatPrice } from "../../../shared/utils/format";
import { getProductsWithStock, updateProduct } from "../../catalog/api";
import { categoryOf, discountPercent } from "../../catalog/catalogView";
import Price from "../../catalog/components/Price";
import type { Product, ProductWithStock } from "../../catalog/types";
import ui from "../admin.module.css";
import styles from "./AdminPromotionsPage.module.css";

// İndirim, ürünün satış fiyatını düşürüp eski fiyatı "indirim öncesi fiyat" olarak saklamaktır.
// Liste fiyatı her zaman indirimsiz fiyattır: indirimli üründe compareAtPrice, diğerlerinde price.
const listPriceOf = (product: Product) => product.compareAtPrice ?? product.price;

const discountedPrice = (listPrice: number, percent: number) => Math.round(listPrice * (1 - percent / 100));

async function saveProductPrice(product: Product, price: number, compareAtPrice: number | null) {
  await updateProduct(product.id, {
    name: product.name,
    description: product.description,
    imageUrl: product.imageUrl,
    category: product.category,
    price,
    compareAtPrice,
  });
}

export default function AdminPromotionsPage() {
  const { data: products = [], error, isLoading, reload } = useAsync(getProductsWithStock, []);
  const toast = useToast();

  const [productId, setProductId] = useState("");
  const [percent, setPercent] = useState("15");
  const [isApplying, setIsApplying] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [removingId, setRemovingId] = useState<string | null>(null);

  const discounted = products
    .filter((p) => discountPercent(p) !== null)
    .sort((a, b) => (discountPercent(b) ?? 0) - (discountPercent(a) ?? 0));
  const sorted = [...products].sort((a, b) => a.name.localeCompare(b.name, "tr"));

  const selected = products.find((p) => p.id === productId);
  const percentNumber = Number(percent);
  const isPercentValid = Number.isInteger(percentNumber) && percentNumber >= 1 && percentNumber <= 90;
  const previewPrice = selected && isPercentValid ? discountedPrice(listPriceOf(selected), percentNumber) : null;

  const apply = async (e: SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!selected || previewPrice === null) return;

    setIsApplying(true);
    setFormError(null);
    try {
      await saveProductPrice(selected, previewPrice, listPriceOf(selected));
      toast.success(`${selected.name}: %${percentNumber} indirim uygulandı.`);
      setProductId("");
      reload();
    } catch (err) {
      setFormError(getErrorMessage(err, "İndirim uygulanamadı."));
    } finally {
      setIsApplying(false);
    }
  };

  const remove = async (product: ProductWithStock) => {
    setRemovingId(product.id);
    try {
      await saveProductPrice(product, listPriceOf(product), null);
      toast.success(`${product.name}: indirim kaldırıldı.`);
      reload();
    } catch (err) {
      toast.error(getErrorMessage(err, "İndirim kaldırılamadı."));
    } finally {
      setRemovingId(null);
    }
  };

  return (
    <>
      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>İndirimler</h1>
          <p className={ui.subtitle}>Ürün bazında indirim uygulayın; mağazada eski fiyat üstü çizili gösterilir.</p>
        </div>
      </div>

      {isLoading && products.length === 0 ? (
        <div className={styles.layout} aria-busy="true" aria-label="İndirimler yükleniyor">
          <Skeleton className={styles.skeleton} />
          <Skeleton className={styles.skeleton} />
        </div>
      ) : error ? (
        <EmptyState
          icon={BadgePercent}
          title="Ürünler yüklenemedi"
          text={getErrorMessage(error, "Catalog servisine ulaşılamıyor.")}
          action={<Button onClick={reload}>Tekrar dene</Button>}
        />
      ) : (
        <div className={styles.layout}>
          <section className={ui.panel}>
            <h2 className={`${ui.panelTitle} ${styles.sectionTitle}`}>
              <Tag size={18} aria-hidden="true" /> Yeni indirim
            </h2>
            <form className={styles.form} onSubmit={apply}>
              {formError && <Alert>{formError}</Alert>}
              <SelectField label="Ürün" required value={productId} onChange={(e) => setProductId(e.target.value)}>
                <option value="">Ürün seçin</option>
                {sorted.map((product) => (
                  <option key={product.id} value={product.id}>
                    {product.name} — {formatPrice(listPriceOf(product))}
                    {discountPercent(product) !== null ? ` (şu an %${discountPercent(product)})` : ""}
                  </option>
                ))}
              </SelectField>
              <FormField
                label="İndirim oranı (%)"
                icon={Percent}
                type="number"
                min={1}
                max={90}
                step={1}
                required
                value={percent}
                onChange={(e) => setPercent(e.target.value)}
                error={percent && !isPercentValid ? "1 ile 90 arasında tam sayı girin." : null}
                hint="İndirim her zaman liste fiyatı üzerinden hesaplanır."
              />

              {selected && previewPrice !== null && (
                <div className={styles.preview}>
                  <Picture src={selected.imageUrl} alt="" width={64} className={styles.previewImage} />
                  <div>
                    <div className={ui.cellTitle}>{selected.name}</div>
                    <Price price={previewPrice} compareAtPrice={listPriceOf(selected)} />
                  </div>
                </div>
              )}

              <Button type="submit" fullWidth loading={isApplying} disabled={!selected || !isPercentValid}>
                {isApplying ? "Uygulanıyor..." : "İndirimi uygula"}
              </Button>
            </form>
          </section>

          <section className={ui.panel}>
            <div className={ui.panelHead}>
              <h2 className={ui.panelTitle}>
                <BadgePercent size={18} aria-hidden="true" /> Aktif indirimler
              </h2>
              <Badge>{discounted.length} ürün</Badge>
            </div>
            {discounted.length === 0 ? (
              <EmptyState variant="plain" icon={BadgePercent} title="Aktif indirim yok" text="Soldaki formla ilk indirimi oluşturabilirsiniz." />
            ) : (
              <ul className={styles.list}>
                {discounted.map((product) => (
                  <li key={product.id} className={styles.item}>
                    <Picture src={product.imageUrl} alt="" width={48} className={styles.thumb} />
                    <div className={styles.itemInfo}>
                      <span className={ui.cellTitle}>{product.name}</span>
                      <span className={ui.cellSub}>{categoryOf(product)}</span>
                    </div>
                    <Badge tone="accent">%{discountPercent(product)}</Badge>
                    <Price price={product.price} compareAtPrice={product.compareAtPrice} />
                    <Button
                      size="sm"
                      variant="ghost"
                      loading={removingId === product.id}
                      onClick={() => remove(product)}
                      aria-label={`${product.name} indirimini kaldır`}
                    >
                      Kaldır
                    </Button>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>
      )}
    </>
  );
}
