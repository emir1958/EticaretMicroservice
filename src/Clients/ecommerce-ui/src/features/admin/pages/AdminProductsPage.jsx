import { useState } from "react";
import { Edit, Image as ImageIcon, Package, Trash2 } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import Alert from "../../../shared/components/Alert";
import Button from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import { formatPrice } from "../../../shared/utils/format";
import { deleteProduct, getProductsWithStock } from "../../catalog/api";
import ProductForm from "../components/ProductForm";
import ui from "../admin.module.css";
import styles from "./AdminProductsPage.module.css";

const LOW_STOCK = 5;

function stockClass(count) {
  if (count <= 0) return styles.stockOut;
  if (count <= LOW_STOCK) return styles.stockLow;
  return styles.stockOk;
}

// Sayfa üç şeyi yönetir: ürün listesi, hangi ürünün düzenlendiği, silme onayı.
// Formun kendi state'i ve kaydetme işi ProductForm'da.
export default function AdminProductsPage() {
  // reload: kaydetme/silme sonrası listeyi yeniden çeker; yenilenirken eski liste ekranda kalır
  const { data: products = [], error: loadError, isLoading, reload } = useAsync(getProductsWithStock, []);

  const [editingProduct, setEditingProduct] = useState(null);
  // Yeni ürün eklendikten sonra boş bir formla başlamak için form "oturumu" sayacı (bkz. formKey)
  const [newFormCount, setNewFormCount] = useState(0);
  const [notice, setNotice] = useState(null); // { tone: "success" | "danger", text }
  const [pendingDeleteId, setPendingDeleteId] = useState(null);
  const [deletingId, setDeletingId] = useState(null);

  // key değişince React eski formu atar, yenisini başlangıç değerleriyle kurar
  const formKey = editingProduct ? `edit-${editingProduct.id}` : `new-${newFormCount}`;

  const startEdit = (product) => {
    setEditingProduct(product);
    setNotice(null);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const handleSaved = (text) => {
    setEditingProduct(null);
    setNewFormCount((count) => count + 1);
    setNotice({ tone: "success", text });
    reload();
  };

  const confirmDelete = async (id) => {
    setDeletingId(id);
    try {
      await deleteProduct(id);
      if (editingProduct?.id === id) setEditingProduct(null);
      setNotice({ tone: "success", text: "Ürün silindi." });
      reload();
    } catch (err) {
      console.error("Ürün silinemedi:", err);
      setNotice({ tone: "danger", text: getErrorMessage(err, "Ürün silinirken bir hata oluştu.") });
    } finally {
      setDeletingId(null);
      setPendingDeleteId(null);
    }
  };

  return (
    <>
      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>Ürün Yönetimi</h1>
          <p className={ui.subtitle}>Katalogdaki ürünler ve Stock servisindeki güncel stokları.</p>
        </div>
      </div>

      {notice && (
        <Alert tone={notice.tone} className={ui.alert}>
          {notice.text}
        </Alert>
      )}

      <ProductForm
        key={formKey}
        product={editingProduct}
        onSaved={handleSaved}
        onCancel={() => setEditingProduct(null)}
      />

      <h2 className={styles.listTitle}>
        Katalogdaki ürünler {!isLoading && <span className={ui.muted}>({products.length})</span>}
      </h2>

      {isLoading && products.length === 0 ? (
        <p className={ui.state}>Yükleniyor...</p>
      ) : loadError ? (
        <Alert>{getErrorMessage(loadError, "Ürünler getirilemedi.")}</Alert>
      ) : products.length === 0 ? (
        <EmptyState icon={Package} title="Henüz ürün yok" text="Yukarıdaki formla ilk ürünü ekleyebilirsin." />
      ) : (
        <div className={ui.tableWrap}>
          <table className={ui.table}>
            <thead>
              <tr>
                <th>
                  <span className="visually-hidden">Görsel</span>
                </th>
                <th>Ürün</th>
                <th>Fiyat</th>
                <th>Stok</th>
                <th className={ui.alignRight}>
                  <span className="visually-hidden">İşlemler</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {products.map((product) => (
                <tr key={product.id} className={editingProduct?.id === product.id ? styles.editingRow : undefined}>
                  <td className={styles.thumbCell}>
                    {product.imageUrl ? (
                      <img src={product.imageUrl} alt="" className={styles.thumb} loading="lazy" />
                    ) : (
                      <div className={styles.thumb}>
                        <ImageIcon size={18} />
                      </div>
                    )}
                  </td>
                  <td>
                    <div className={ui.strong}>{product.name}</div>
                    <div className={`${ui.muted} ${styles.description}`}>{product.description || "—"}</div>
                  </td>
                  <td className={`${ui.strong} ${ui.nowrap}`}>{formatPrice(product.price)}</td>
                  <td>
                    <span className={`${styles.stock} ${stockClass(product.availableStock)}`}>
                      {product.availableStock} adet
                    </span>
                  </td>
                  <td className={ui.alignRight}>
                    {pendingDeleteId === product.id ? (
                      // Silme geri alınamaz: tarayıcının confirm() penceresi yerine satır içinde onay
                      <div className={styles.actions}>
                        <span className={styles.confirmText}>Silinsin mi?</span>
                        <Button
                          variant="danger"
                          size="sm"
                          disabled={deletingId === product.id}
                          onClick={() => confirmDelete(product.id)}
                        >
                          {deletingId === product.id ? "Siliniyor..." : "Evet, sil"}
                        </Button>
                        <Button variant="ghost" size="sm" onClick={() => setPendingDeleteId(null)}>
                          Vazgeç
                        </Button>
                      </div>
                    ) : (
                      <div className={styles.actions}>
                        <Button variant="secondary" size="sm" onClick={() => startEdit(product)}>
                          <Edit size={14} /> Düzenle
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setPendingDeleteId(product.id)}
                          aria-label={`${product.name} ürününü sil`}
                        >
                          <Trash2 size={14} /> Sil
                        </Button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
