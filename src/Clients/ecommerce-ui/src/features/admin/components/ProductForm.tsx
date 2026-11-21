import { useEffect, useId, useState, type SubmitEvent } from "react";
import { getErrorMessage } from "../../../lib/apiClient";
import Alert from "../../../shared/components/Alert";
import FormField, { TextAreaField } from "../../../shared/components/FormField";
import Picture from "../../../shared/components/Picture";
import { createProduct, updateProduct } from "../../catalog/api";
import { UNCATEGORIZED } from "../../catalog/catalogView";
import { getAvailableStock, updateStock } from "../../stock/api";
import type { ProductWithStock } from "../../catalog/types";
import styles from "./ProductForm.module.css";

// Ürün ekleme / düzenleme formu.
// product: düzenlenecek ürün (null → yeni ürün). Sayfa bu bileşeni ürünün id'siyle key'ler:
// başka ürüne geçilince React formu sıfırdan oluşturur, state'i elle sıfırlamak gerekmez.
interface ProductFormProps {
  product: ProductWithStock | null;
  // Mevcut kategoriler: yazarken öneri olarak gösterilir, yeni kategori de yazılabilir
  categories: string[];
  formId: string;
  onSaved: (message: string) => void;
  onSavingChange: (isSaving: boolean) => void;
}

const toNumber = (value: string) => (value.trim() === "" ? null : Number(value));

export default function ProductForm({ product, categories, formId, onSaved, onSavingChange }: ProductFormProps) {
  const isEdit = product !== null;
  const productId = product?.id;
  const categoryListId = useId();

  // useState başlangıç değeri yalnızca ilk render'da okunur; key değişimi bu yüzden önemli.
  const [name, setName] = useState(product?.name ?? "");
  const [category, setCategory] = useState(product?.category ?? "");
  const [price, setPrice] = useState(product?.price?.toString() ?? "");
  const [compareAtPrice, setCompareAtPrice] = useState(product?.compareAtPrice?.toString() ?? "");
  const [stock, setStock] = useState(product?.availableStock?.toString() ?? "");
  // Forma yüklenen stok: kaydederken kullanıcı bu alanı değiştirmediyse Stock servisine dokunulmaz
  const [loadedStock, setLoadedStock] = useState(stock);
  const [imageUrl, setImageUrl] = useState(product?.imageUrl ?? "");
  const [description, setDescription] = useState(product?.description ?? "");
  const [error, setError] = useState<string | null>(null);

  // Listedeki stok liste yüklendiği anki değer; düzenlemeye başlarken Stock servisinden tazelenir.
  // Bağımlılık product değil productId: liste yenilenince ürün nesnesi değişir ama id aynı kalır,
  // effect gereksiz yere tekrar çalışıp kullanıcının yazdığı değeri ezmez.
  useEffect(() => {
    if (!productId) return;
    let ignore = false;
    getAvailableStock(productId)
      .then((value) => {
        if (ignore) return;
        setStock(String(value));
        setLoadedStock(String(value));
      })
      .catch((err) => console.warn("Güncel stok alınamadı, listedeki değer kullanılıyor:", err));
    return () => {
      ignore = true;
    };
  }, [productId]);

  const priceNumber = toNumber(price);
  const compareNumber = toNumber(compareAtPrice);
  const compareError =
    compareNumber !== null && priceNumber !== null && compareNumber <= priceNumber
      ? "İndirim öncesi fiyat, satış fiyatından yüksek olmalı."
      : null;

  const handleSubmit = async (e: SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (compareError) return;

    onSavingChange(true);
    setError(null);

    const fields = {
      name: name.trim(),
      category: category.trim(),
      description: description.trim(),
      price: Number(price),
      compareAtPrice: compareNumber,
      imageUrl: imageUrl.trim(),
    };
    const stockNumber = Number.parseInt(stock, 10) || 0;

    try {
      if (isEdit) {
        // Ürün bilgisi Catalog'da, stok Stock servisinde. Form açıkken siparişler stoğu değiştirmiş olabilir;
        // bu yüzden stok yalnızca kullanıcı alanı değiştirdiyse yazılır (fiyat düzenlemesi stoğu ezmez).
        await updateProduct(product.id, fields);
        if (stock !== loadedStock) await updateStock(product.id, stockNumber);
        onSaved(`"${fields.name}" güncellendi.`);
      } else {
        // Başlangıç stoğu Catalog'dan event ile Stock servisine iletilir
        await createProduct({ ...fields, initialStock: stockNumber });
        onSaved(`"${fields.name}" kataloğa eklendi.`);
      }
    } catch (err) {
      console.error("Ürün kaydedilemedi:", err);
      setError(getErrorMessage(err, "İşlem gerçekleştirilemedi."));
    } finally {
      onSavingChange(false);
    }
  };

  return (
    <form id={formId} onSubmit={handleSubmit} className={styles.form}>
      {error && <Alert>{error}</Alert>}

      <div className={styles.preview}>
        <Picture src={imageUrl.trim()} alt={name || "Ürün görseli"} width={160} className={styles.previewImage} />
        <FormField
          className={styles.grow}
          label="Görsel URL"
          type="url"
          value={imageUrl}
          onChange={(e) => setImageUrl(e.target.value)}
          placeholder="https://..."
          hint="Önizleme soldaki kutuda görünür."
        />
      </div>

      <FormField
        label="Ürün adı"
        required
        maxLength={120}
        value={name}
        onChange={(e) => setName(e.target.value)}
        placeholder="Örn: Tık 75 Mekanik Klavye"
      />

      <FormField
        label="Kategori"
        list={categoryListId}
        value={category}
        onChange={(e) => setCategory(e.target.value)}
        placeholder="Listeden seç veya yeni kategori yaz"
        hint={`Boş bırakılırsa ürün mağazada "${UNCATEGORIZED}" altında görünür.`}
      />
      <datalist id={categoryListId}>
        {categories.map((name) => (
          <option key={name} value={name} />
        ))}
      </datalist>

      <div className={styles.row}>
        <FormField
          label="Satış fiyatı (TL)"
          type="number"
          min="0.01"
          step="0.01"
          required
          value={price}
          onChange={(e) => setPrice(e.target.value)}
          placeholder="3499.00"
        />
        <FormField
          label="İndirim öncesi fiyat (TL)"
          type="number"
          min="0"
          step="0.01"
          value={compareAtPrice}
          onChange={(e) => setCompareAtPrice(e.target.value)}
          placeholder="İsteğe bağlı"
          error={compareError}
          hint="Doluysa üstü çizili gösterilir."
        />
      </div>

      <FormField
        label={isEdit ? "Satılabilir stok" : "Başlangıç stoğu"}
        type="number"
        min="0"
        step="1"
        required
        value={stock}
        onChange={(e) => setStock(e.target.value)}
        placeholder="100"
        hint={isEdit ? "Ödeme bekleyen siparişlere ayrılmış adetler bu sayıya dahil değildir." : undefined}
      />

      <TextAreaField
        label="Açıklama"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        placeholder="Ürünü müşterinin gözünden anlatan kısa bir açıklama"
        rows={5}
      />
    </form>
  );
}
