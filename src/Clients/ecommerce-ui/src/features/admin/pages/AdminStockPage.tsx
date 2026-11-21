import { useState } from "react";
import { AlertTriangle, Boxes, Lock, PackageX, SearchX } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import { useToast } from "../../../shared/toast/ToastContext";
import Badge from "../../../shared/components/Badge";
import Button from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import FilterTabs from "../../../shared/components/FilterTabs";
import Picture from "../../../shared/components/Picture";
import SearchInput from "../../../shared/components/SearchInput";
import Skeleton from "../../../shared/components/Skeleton";
import { formatNumber } from "../../../shared/utils/format";
import { getProducts } from "../../catalog/api";
import { categoryOf, LOW_STOCK_THRESHOLD, normalizeText, stockStateOf, type StockState } from "../../catalog/catalogView";
import { getStocks, updateStock } from "../../stock/api";
import StatCard from "../components/StatCard";
import ui from "../admin.module.css";
import styles from "./AdminStockPage.module.css";

type Filter = "all" | StockState;

const TABS: { value: Filter; label: string }[] = [
  { value: "all", label: "Tümü" },
  { value: "low", label: "Az kalan" },
  { value: "out", label: "Tükenen" },
];

const STATE_BADGE = {
  in: { tone: "success", label: "Yeterli" },
  low: { tone: "warning", label: "Az kaldı" },
  out: { tone: "danger", label: "Tükendi" },
} as const;

const withoutKey = (record: Record<string, string>, key: string) => {
  const next = { ...record };
  delete next[key];
  return next;
};

// Katalog ürünleri ile Stock servisindeki kayıtlar ürün kimliği üzerinden birleştirilir.
// Stok kaydı olmayan ürün 0 adet kabul edilir (ilk kayıt bu sayfadan kaydedilince açılır).
async function loadStock() {
  const [products, stocks] = await Promise.all([getProducts(), getStocks()]);
  const stockById = new Map(stocks.map((s) => [s.productId, s]));
  return products.map((product) => ({
    product,
    available: stockById.get(product.id)?.availableStock ?? 0,
    reserved: stockById.get(product.id)?.reservedStock ?? 0,
  }));
}

export default function AdminStockPage() {
  const { data: rows = [], error, isLoading, reload } = useAsync(loadStock, []);
  const toast = useToast();
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState<Filter>("all");
  // Kaydedilmemiş değişiklikler: ürün kimliği → yazılan değer
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [savingId, setSavingId] = useState<string | null>(null);

  const terms = normalizeText(query).split(/\s+/).filter(Boolean);
  const searched = rows.filter((row) => {
    const haystack = normalizeText(`${row.product.name} ${row.product.category}`);
    return terms.every((term) => haystack.includes(term));
  });
  const visible = searched
    .filter((row) => filter === "all" || stockStateOf(row.available) === filter)
    .sort((a, b) => a.available - b.available);

  const lowCount = rows.filter((r) => stockStateOf(r.available) === "low").length;
  const outCount = rows.filter((r) => stockStateOf(r.available) === "out").length;

  const save = async (productId: string, name: string) => {
    const value = Number.parseInt(drafts[productId], 10);
    if (Number.isNaN(value) || value < 0) return;

    setSavingId(productId);
    try {
      await updateStock(productId, value);
      setDrafts((current) => withoutKey(current, productId));
      toast.success(`${name}: stok ${value} adet olarak güncellendi.`);
      reload();
    } catch (err) {
      toast.error(getErrorMessage(err, "Stok güncellenemedi."));
    } finally {
      setSavingId(null);
    }
  };

  return (
    <>
      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>Stok takibi</h1>
          <p className={ui.subtitle}>
            Satılabilir adetler. Sipariş verildiğinde ürün rezerve edilir, ödeme alınınca stoktan düşer.
          </p>
        </div>
      </div>

      {isLoading && rows.length === 0 ? (
        <div aria-busy="true" aria-label="Stok yükleniyor">
          <div className={ui.stats}>
            {[1, 2, 3, 4].map((i) => (
              <Skeleton key={i} className={ui.skeletonStat} />
            ))}
          </div>
          {Array.from({ length: 6 }, (_, i) => (
            <Skeleton key={i} className={ui.skeletonRow} />
          ))}
        </div>
      ) : error ? (
        <EmptyState
          icon={Boxes}
          title="Stok bilgisi yüklenemedi"
          text={getErrorMessage(error, "Stock veya Catalog servisine ulaşılamıyor.")}
          action={<Button onClick={reload}>Tekrar dene</Button>}
        />
      ) : (
        <>
          <div className={ui.stats}>
            <StatCard icon={Boxes} label="Satılabilir toplam" value={`${formatNumber(rows.reduce((s, r) => s + r.available, 0))} adet`} />
            <StatCard
              icon={Lock}
              label="Ödeme bekleyen"
              value={`${formatNumber(rows.reduce((s, r) => s + r.reserved, 0))} adet`}
              hint="Siparişler için ayrılmış"
            />
            <StatCard icon={AlertTriangle} label="Az kalan ürün" value={formatNumber(lowCount)} hint={`${LOW_STOCK_THRESHOLD} adet ve altı`} />
            <StatCard icon={PackageX} label="Tükenen ürün" value={formatNumber(outCount)} />
          </div>

          <div className={ui.toolbar}>
            <SearchInput className={ui.toolbarSearch} value={query} onChange={setQuery} label="Stokta ara" placeholder="Ürün adı" />
            <FilterTabs
              label="Stok durumu"
              tabs={TABS.map((t) => ({
                ...t,
                count: t.value === "all" ? searched.length : searched.filter((r) => stockStateOf(r.available) === t.value).length,
              }))}
              value={filter}
              onChange={setFilter}
            />
          </div>

          {visible.length === 0 ? (
            <EmptyState icon={SearchX} title="Eşleşen ürün yok" />
          ) : (
            <div className={ui.tableWrap}>
              <table className={ui.table}>
                <thead>
                  <tr>
                    <th>Ürün</th>
                    <th>Durum</th>
                    <th className={ui.alignRight}>Rezerve</th>
                    <th>Satılabilir adet</th>
                  </tr>
                </thead>
                <tbody>
                  {visible.map(({ product, available, reserved }) => {
                    const state = STATE_BADGE[stockStateOf(available)];
                    const draft = drafts[product.id];
                    const isDirty = draft !== undefined && draft !== String(available);
                    const isValid = draft === undefined || /^\d+$/.test(draft);
                    return (
                      <tr key={product.id}>
                        <td>
                          <div className={styles.product}>
                            <Picture src={product.imageUrl} alt="" width={40} className={styles.thumb} />
                            <div>
                              <div className={ui.cellTitle}>{product.name}</div>
                              <div className={ui.cellSub}>{categoryOf(product)}</div>
                            </div>
                          </div>
                        </td>
                        <td>
                          <Badge tone={state.tone} dot>
                            {state.label}
                          </Badge>
                        </td>
                        <td className={`${ui.alignRight} ${ui.num} ${ui.muted}`}>{reserved}</td>
                        <td>
                          <form
                            className={styles.editor}
                            onSubmit={(e) => {
                              e.preventDefault();
                              save(product.id, product.name);
                            }}
                          >
                            <input
                              className={styles.input}
                              type="number"
                              min={0}
                              step={1}
                              inputMode="numeric"
                              aria-label={`${product.name} satılabilir adet`}
                              aria-invalid={!isValid || undefined}
                              value={draft ?? String(available)}
                              onChange={(e) => setDrafts((current) => ({ ...current, [product.id]: e.target.value }))}
                            />
                            {isDirty && (
                              <>
                                <Button type="submit" size="sm" disabled={!isValid} loading={savingId === product.id}>
                                  Kaydet
                                </Button>
                                <Button
                                  size="sm"
                                  variant="ghost"
                                  onClick={() => setDrafts((current) => withoutKey(current, product.id))}
                                >
                                  Geri al
                                </Button>
                              </>
                            )}
                          </form>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}
    </>
  );
}
