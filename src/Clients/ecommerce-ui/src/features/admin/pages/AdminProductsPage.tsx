import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ExternalLink, Package, Pencil, Plus, SearchX, Trash2 } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import { useToast } from "../../../shared/toast/ToastContext";
import Badge from "../../../shared/components/Badge";
import Button from "../../../shared/components/Button";
import Dialog from "../../../shared/components/Dialog";
import EmptyState from "../../../shared/components/EmptyState";
import FilterTabs from "../../../shared/components/FilterTabs";
import IconButton from "../../../shared/components/IconButton";
import Picture from "../../../shared/components/Picture";
import SearchInput from "../../../shared/components/SearchInput";
import Skeleton from "../../../shared/components/Skeleton";
import { deleteProduct, getProductsWithStock } from "../../catalog/api";
import {
  categoryOf,
  discountPercent,
  listCategories,
  normalizeText,
  stockStateOf,
  UNCATEGORIZED,
  type StockState,
} from "../../catalog/catalogView";
import Price from "../../catalog/components/Price";
import type { ProductWithStock } from "../../catalog/types";
import ProductForm from "../components/ProductForm";
import ui from "../admin.module.css";
import styles from "./AdminProductsPage.module.css";

const FORM_ID = "product-form";

type StockFilter = "all" | StockState;

const STOCK_TABS: { value: StockFilter; label: string }[] = [
  { value: "all", label: "Tümü" },
  { value: "in", label: "Stokta" },
  { value: "low", label: "Az kalan" },
  { value: "out", label: "Tükenen" },
];

const STOCK_BADGE = {
  in: { tone: "success", label: (n: number) => `${n} adet` },
  low: { tone: "warning", label: (n: number) => `${n} adet` },
  out: { tone: "danger", label: () => "Tükendi" },
} as const;

// Düzenleme formu: kapalı, yeni ürün veya belirli bir ürün
type Editor = { mode: "closed" } | { mode: "new" } | { mode: "edit"; product: ProductWithStock };

export default function AdminProductsPage() {
  // reload: kaydetme/silme sonrası listeyi yeniden çeker; yenilenirken eski liste ekranda kalır
  const { data: products = [], error, isLoading, reload } = useAsync(getProductsWithStock, []);
  const toast = useToast();
  const [searchParams, setSearchParams] = useSearchParams();

  const [editor, setEditor] = useState<Editor>({ mode: "closed" });
  const [isSaving, setIsSaving] = useState(false);
  const [pendingDelete, setPendingDelete] = useState<ProductWithStock | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const query = searchParams.get("q") ?? "";
  const category = searchParams.get("category") ?? "";
  const stockFilter = STOCK_TABS.find((t) => t.value === searchParams.get("stock"))?.value ?? "all";

  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(searchParams);
    if (value) next.set(key, value);
    else next.delete(key);
    setSearchParams(next, { replace: true });
  };

  const categories = listCategories(products);
  const terms = normalizeText(query).split(/\s+/).filter(Boolean);
  const searched = products.filter((p) => {
    if (category && categoryOf(p) !== category) return false;
    const haystack = normalizeText(`${p.name} ${p.category}`);
    return terms.every((term) => haystack.includes(term));
  });
  const visible = searched
    .filter((p) => stockFilter === "all" || stockStateOf(p.availableStock) === stockFilter)
    .sort((a, b) => a.name.localeCompare(b.name, "tr"));

  const closeEditor = () => {
    if (!isSaving) setEditor({ mode: "closed" });
  };

  const handleSaved = (message: string) => {
    setEditor({ mode: "closed" });
    toast.success(message);
    reload();
  };

  const confirmDelete = async () => {
    if (!pendingDelete) return;
    setIsDeleting(true);
    setDeleteError(null);
    try {
      await deleteProduct(pendingDelete.id);
      toast.success(`"${pendingDelete.name}" silindi.`);
      setPendingDelete(null);
      reload();
    } catch (err) {
      setDeleteError(getErrorMessage(err, "Ürün silinirken bir hata oluştu."));
    } finally {
      setIsDeleting(false);
    }
  };

  const editingProduct = editor.mode === "edit" ? editor.product : null;

  return (
    <>
      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>Ürünler</h1>
          <p className={ui.subtitle}>Katalog bilgileri Catalog servisinde, stok adetleri Stock servisinde tutulur.</p>
        </div>
        <Button onClick={() => setEditor({ mode: "new" })}>
          <Plus size={18} aria-hidden="true" /> Yeni ürün
        </Button>
      </div>

      <div className={ui.toolbar}>
        <SearchInput
          className={ui.toolbarSearch}
          value={query}
          onChange={(value) => setParam("q", value || null)}
          label="Ürünlerde ara"
          placeholder="Ürün adı veya kategori"
        />
        <select
          className={ui.toolbarSelect}
          value={category}
          onChange={(e) => setParam("category", e.target.value || null)}
          aria-label="Kategori"
        >
          <option value="">Tüm kategoriler</option>
          {categories.map((c) => (
            <option key={c.name} value={c.name}>
              {c.name} ({c.count})
            </option>
          ))}
        </select>
        <FilterTabs
          label="Stok durumu"
          tabs={STOCK_TABS.map((t) => ({
            ...t,
            count: t.value === "all" ? searched.length : searched.filter((p) => stockStateOf(p.availableStock) === t.value).length,
          }))}
          value={stockFilter}
          onChange={(value) => setParam("stock", value === "all" ? null : value)}
        />
      </div>

      {isLoading && products.length === 0 ? (
        <div aria-busy="true" aria-label="Ürünler yükleniyor">
          {Array.from({ length: 8 }, (_, i) => (
            <Skeleton key={i} className={ui.skeletonRow} />
          ))}
        </div>
      ) : error ? (
        <EmptyState
          icon={Package}
          title="Ürünler yüklenemedi"
          text={getErrorMessage(error, "Catalog servisine ulaşılamıyor.")}
          action={<Button onClick={reload}>Tekrar dene</Button>}
        />
      ) : products.length === 0 ? (
        <EmptyState
          icon={Package}
          title="Katalog boş"
          text="İlk ürünü ekleyerek mağazayı açmaya başlayabilirsin."
          action={<Button onClick={() => setEditor({ mode: "new" })}>Ürün ekle</Button>}
        />
      ) : visible.length === 0 ? (
        <EmptyState
          icon={SearchX}
          title="Eşleşen ürün yok"
          action={
            <Button variant="secondary" onClick={() => setSearchParams({}, { replace: true })}>
              Filtreleri temizle
            </Button>
          }
        />
      ) : (
        <div className={ui.tableWrap}>
          <table className={ui.table}>
            <thead>
              <tr>
                <th>Ürün</th>
                <th>Kategori</th>
                <th className={ui.alignRight}>Fiyat</th>
                <th>Stok</th>
                <th className={ui.alignRight}>
                  <span className="visually-hidden">İşlemler</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {visible.map((product) => {
                const state = stockStateOf(product.availableStock);
                const badge = STOCK_BADGE[state];
                const discount = discountPercent(product);
                return (
                  <tr key={product.id}>
                    <td>
                      <div className={styles.product}>
                        <Picture src={product.imageUrl} alt="" width={48} className={styles.thumb} />
                        <div className={styles.productText}>
                          <span className={ui.cellTitle}>{product.name}</span>
                          <span className={ui.cellSub}>
                            {discount !== null && <Badge tone="accent">%{discount}</Badge>}{" "}
                            {product.isSample && <Badge>Örnek</Badge>}
                          </span>
                        </div>
                      </div>
                    </td>
                    <td className={categoryOf(product) === UNCATEGORIZED ? ui.muted : undefined}>{categoryOf(product)}</td>
                    <td className={`${ui.alignRight} ${ui.nowrap}`}>
                      <Price price={product.price} compareAtPrice={product.compareAtPrice} />
                    </td>
                    <td>
                      <Badge tone={badge.tone} dot>
                        {badge.label(product.availableStock)}
                      </Badge>
                    </td>
                    <td className={ui.alignRight}>
                      <div className={styles.actions}>
                        <Link
                          to={`/product/${product.id}`}
                          target="_blank"
                          className={styles.iconLink}
                          aria-label={`${product.name} ürününü mağazada aç`}
                          title="Mağazada aç"
                        >
                          <ExternalLink size={16} aria-hidden="true" />
                        </Link>
                        <IconButton
                          icon={Pencil}
                          size="sm"
                          label={`${product.name} ürününü düzenle`}
                          onClick={() => setEditor({ mode: "edit", product })}
                        />
                        <IconButton
                          icon={Trash2}
                          size="sm"
                          label={`${product.name} ürününü sil`}
                          onClick={() => {
                            setDeleteError(null);
                            setPendingDelete(product);
                          }}
                        />
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <Dialog
        open={editor.mode !== "closed"}
        onClose={closeEditor}
        title={editingProduct ? "Ürünü düzenle" : "Yeni ürün"}
        size="lg"
        footer={
          <div className={styles.footer}>
            <Button variant="ghost" onClick={closeEditor} disabled={isSaving}>
              Vazgeç
            </Button>
            <Button type="submit" form={FORM_ID} loading={isSaving}>
              {isSaving ? "Kaydediliyor..." : editingProduct ? "Değişiklikleri kaydet" : "Ürünü ekle"}
            </Button>
          </div>
        }
      >
        {editor.mode !== "closed" && (
          <ProductForm
            key={editingProduct?.id ?? "new"}
            formId={FORM_ID}
            product={editingProduct}
            categories={categories.map((c) => c.name).filter((name) => name !== UNCATEGORIZED)}
            onSaved={handleSaved}
            onSavingChange={setIsSaving}
          />
        )}
      </Dialog>

      {/* Silme geri alınamaz: tarayıcının confirm() penceresi yerine kendi onay penceremiz */}
      <Dialog
        open={pendingDelete !== null}
        onClose={() => !isDeleting && setPendingDelete(null)}
        title="Ürün silinsin mi?"
        placement="center"
        size="sm"
        footer={
          <div className={styles.footer}>
            <Button variant="ghost" onClick={() => setPendingDelete(null)} disabled={isDeleting}>
              Vazgeç
            </Button>
            <Button variant="danger" onClick={confirmDelete} loading={isDeleting}>
              {isDeleting ? "Siliniyor..." : "Evet, sil"}
            </Button>
          </div>
        }
      >
        <p className={styles.confirmText}>
          <strong>{pendingDelete?.name}</strong> katalogdan kaldırılacak. Geçmiş siparişlerdeki kayıtlar korunur, ancak ürün
          mağazada artık görünmez. Bu işlem geri alınamaz.
        </p>
        {deleteError && <p className={styles.confirmError}>{deleteError}</p>}
      </Dialog>
    </>
  );
}
