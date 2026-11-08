import { useEffect, useState } from "react";
import { Edit, PlusCircle, X } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import Alert from "../../../shared/components/Alert";
import Button from "../../../shared/components/Button";
import FormField from "../../../shared/components/FormField";
import { createProduct, getAvailableStock, updateProduct, updateStock } from "../../catalog/api";
import ui from "../admin.module.css";
import styles from "./ProductForm.module.css";

// Ürün ekleme / düzenleme formu.
// product: düzenlenecek ürün (null → yeni ürün). Sayfa bu bileşeni ürünün id'siyle key'ler:
// başka ürüne geçilince React formu sıfırdan oluşturur, state'i elle sıfırlamak gerekmez.
export default function ProductForm({ product, onSaved, onCancel }) {
  const isEdit = Boolean(product);
  const productId = product?.id;

  // useState başlangıç değeri yalnızca ilk render'da okunur; key değişimi bu yüzden önemli.
  const [name, setName] = useState(product?.name ?? "");
  const [price, setPrice] = useState(product?.price?.toString() ?? "");
  const [stock, setStock] = useState(product?.availableStock?.toString() ?? "");
  const [imageUrl, setImageUrl] = useState(product?.imageUrl ?? "");
  const [description, setDescription] = useState(product?.description ?? "");
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState(null);

  // Listedeki stok liste yüklendiği anki değer; düzenlemeye başlarken Stock servisinden tazelenir.
  // Bağımlılık product değil productId: liste yenilenince ürün nesnesi değişir ama id aynı kalır,
  // effect gereksiz yere tekrar çalışıp kullanıcının yazdığı değeri ezmez.
  useEffect(() => {
    if (!productId) return;
    let ignore = false;
    getAvailableStock(productId)
      .then((value) => {
        if (!ignore) setStock(String(value));
      })
      .catch((err) => console.warn("Güncel stok alınamadı, listedeki değer kullanılıyor:", err));
    return () => {
      ignore = true;
    };
  }, [productId]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsSaving(true);
    setError(null);

    const fields = {
      name: name.trim(),
      description: description.trim(),
      price: Number(price),
      imageUrl: imageUrl.trim(),
    };
    const stockNumber = Number.parseInt(stock, 10) || 0;

    try {
      if (isEdit) {
        // Ürün bilgisi Catalog'da, stok Stock servisinde: iki ayrı servis, iki ayrı istek
        await updateProduct(productId, fields);
        await updateStock(productId, stockNumber);
        onSaved("Ürün bilgileri ve stok güncellendi.");
      } else {
        // Başlangıç stoğu Catalog'dan event ile Stock servisine iletilir
        await createProduct({ ...fields, initialStock: stockNumber });
        onSaved("Yeni ürün eklendi.");
      }
    } catch (err) {
      console.error("Ürün kaydedilemedi:", err);
      setError(getErrorMessage(err, "İşlem gerçekleştirilemedi."));
      setIsSaving(false);
    }
    // Başarıda setIsSaving(false) yok: onSaved sonrası sayfa formu yeni key ile yeniden oluşturur.
  };

  return (
    <section className={ui.panel}>
      <div className={styles.head}>
        <h2 className={ui.panelTitle}>
          {isEdit ? <Edit size={20} /> : <PlusCircle size={20} />}
          {isEdit ? `Düzenle: ${product.name}` : "Yeni ürün"}
        </h2>
        {isEdit && (
          <Button variant="ghost" size="sm" onClick={onCancel}>
            <X size={16} /> Vazgeç
          </Button>
        )}
      </div>

      {error && <Alert className={ui.alert}>{error}</Alert>}

      <form onSubmit={handleSubmit} className={styles.grid}>
        <FormField
          className={styles.wide}
          label="Ürün adı"
          required
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Örn: Logi MX Master 3S"
        />
        <FormField
          label="Fiyat (TL)"
          type="number"
          min="0"
          step="0.01"
          required
          value={price}
          onChange={(e) => setPrice(e.target.value)}
          placeholder="3499.00"
        />
        <FormField
          label={isEdit ? "Güncel stok" : "Başlangıç stoğu"}
          type="number"
          min="0"
          step="1"
          required
          value={stock}
          onChange={(e) => setStock(e.target.value)}
          placeholder="100"
        />
        <FormField
          className={styles.full}
          label="Görsel URL"
          type="url"
          value={imageUrl}
          onChange={(e) => setImageUrl(e.target.value)}
          placeholder="https://..."
        />
        <FormField
          className={styles.full}
          as="textarea"
          label="Açıklama"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Kısa ürün açıklaması"
        />

        <div className={styles.full}>
          <Button type="submit" disabled={isSaving}>
            {isSaving ? "Kaydediliyor..." : isEdit ? "Değişiklikleri kaydet" : "Ürünü ekle"}
          </Button>
        </div>
      </form>
    </section>
  );
}
