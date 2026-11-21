import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { SearchX, SlidersHorizontal, X } from "lucide-react";
import { useAsync } from "../../../shared/hooks/useAsync";
import Breadcrumb from "../../../shared/components/Breadcrumb";
import Button from "../../../shared/components/Button";
import Dialog from "../../../shared/components/Dialog";
import EmptyState from "../../../shared/components/EmptyState";
import Pagination from "../../../shared/components/Pagination";
import { getProductsWithStock } from "../api";
import {
  filterProducts,
  listCategories,
  PRICE_RANGES,
  SORT_OPTIONS,
  sortProducts,
  type ProductFilters,
  type SortKey,
} from "../catalogView";
import ProductGrid from "../components/ProductGrid";
import type { Category } from "../types";
import styles from "./ProductListPage.module.css";

const PAGE_SIZE = 12;

// Filtre, sıralama ve sayfa bilgisi adres çubuğunda tutulur (?category=Ses&sort=price-asc):
// sayfa yenilenince kaybolmaz, bağlantı olarak paylaşılabilir, tarayıcının geri tuşu çalışır.
// Bu yüzden ayrıca state'e kopyalanmaz; her render'da adresten okunur.
export default function ProductListPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const { data: products = [], error, isLoading, reload } = useAsync(getProductsWithStock, []);
  const [isFilterOpen, setIsFilterOpen] = useState(false);

  const filters: ProductFilters = {
    query: searchParams.get("q") ?? "",
    category: searchParams.get("category") ?? "",
    priceRange: searchParams.get("price") ?? "",
    inStockOnly: searchParams.get("stock") === "1",
    onSaleOnly: searchParams.get("sale") === "1",
  };
  const sort: SortKey = SORT_OPTIONS.find((o) => o.value === searchParams.get("sort"))?.value ?? "featured";

  // Filtre değişince ilk sayfaya dönülür
  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(searchParams);
    if (value) next.set(key, value);
    else next.delete(key);
    if (key !== "page") next.delete("page");
    setSearchParams(next);
  };

  const clearFilters = () => {
    const next = new URLSearchParams();
    if (sort !== "featured") next.set("sort", sort);
    setSearchParams(next);
  };

  const visible = sortProducts(filterProducts(products, filters), sort);
  const pageCount = Math.max(1, Math.ceil(visible.length / PAGE_SIZE));
  const page = Math.min(Math.max(1, Number(searchParams.get("page")) || 1), pageCount);
  const pageItems = visible.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  // Kategori sayıları diğer filtrelere göre hesaplanır (seçili kategorinin kendisi hariç)
  const categories = listCategories(filterProducts(products, { ...filters, category: "" }));

  const priceLabel = PRICE_RANGES.find((r) => r.value === filters.priceRange)?.label;
  const chips: { key: string; label: string }[] = [];
  if (filters.query) chips.push({ key: "q", label: `"${filters.query}"` });
  if (filters.category) chips.push({ key: "category", label: filters.category });
  if (priceLabel) chips.push({ key: "price", label: priceLabel });
  if (filters.inStockOnly) chips.push({ key: "stock", label: "Stoktakiler" });
  if (filters.onSaleOnly) chips.push({ key: "sale", label: "İndirimdekiler" });

  const title = filters.category || (filters.query ? `"${filters.query}" için sonuçlar` : "Tüm ürünler");
  const crumbs = filters.category
    ? [{ label: "Ana sayfa", to: "/" }, { label: "Ürünler", to: "/products" }, { label: filters.category }]
    : [{ label: "Ana sayfa", to: "/" }, { label: "Ürünler" }];

  const filterPanel = (
    <FilterPanel filters={filters} categories={categories} totalCount={products.length} onChange={setParam} />
  );

  return (
    <>
      <Breadcrumb items={crumbs} />

      <header className={styles.header}>
        <h1 className={styles.title}>{title}</h1>
        {!isLoading && !error && <p className={styles.count}>{visible.length} ürün</p>}
      </header>

      <div className={styles.layout}>
        <aside className={styles.sidebar} aria-label="Filtreler">
          {filterPanel}
        </aside>

        <div className={styles.results}>
          <div className={styles.toolbar}>
            <Button variant="secondary" size="sm" className={styles.filterButton} onClick={() => setIsFilterOpen(true)}>
              <SlidersHorizontal size={16} aria-hidden="true" /> Filtreler
              {chips.length > 0 && <span className={styles.filterCount}>{chips.length}</span>}
            </Button>

            <label className={styles.sort}>
              <span className={styles.sortLabel}>Sırala</span>
              <select
                className={styles.sortSelect}
                value={sort}
                onChange={(e) => setParam("sort", e.target.value === "featured" ? null : e.target.value)}
              >
                {SORT_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
          </div>

          {chips.length > 0 && (
            <div className={styles.chips}>
              {chips.map((chip) => (
                <button key={chip.key} type="button" className={styles.chip} onClick={() => setParam(chip.key, null)}>
                  {chip.label}
                  <X size={14} aria-hidden="true" />
                  <span className="visually-hidden">filtresini kaldır</span>
                </button>
              ))}
              <button type="button" className={styles.clear} onClick={clearFilters}>
                Tümünü temizle
              </button>
            </div>
          )}

          {error ? (
            <EmptyState
              icon={SearchX}
              title="Ürünler yüklenemedi"
              text="Katalog servisine şu anda ulaşılamıyor. Birkaç saniye sonra tekrar deneyin."
              action={<Button onClick={reload}>Tekrar dene</Button>}
            />
          ) : !isLoading && visible.length === 0 ? (
            <EmptyState
              icon={SearchX}
              title="Aradığın kriterlerde ürün bulamadık"
              text="Filtreleri azaltmayı veya farklı bir kelimeyle aramayı deneyebilirsin."
              action={<Button onClick={clearFilters}>Filtreleri temizle</Button>}
            />
          ) : (
            <>
              <ProductGrid products={pageItems} isLoading={isLoading} skeletonCount={6} />
              <div className={styles.pagination}>
                <Pagination
                  page={page}
                  pageCount={pageCount}
                  onChange={(nextPage) => {
                    setParam("page", nextPage > 1 ? String(nextPage) : null);
                    window.scrollTo({ top: 0 });
                  }}
                />
              </div>
            </>
          )}
        </div>
      </div>

      <Dialog
        open={isFilterOpen}
        onClose={() => setIsFilterOpen(false)}
        title="Filtreler"
        placement="left"
        size="sm"
        footer={
          <Button fullWidth onClick={() => setIsFilterOpen(false)}>
            {visible.length} ürünü göster
          </Button>
        }
      >
        {filterPanel}
      </Dialog>
    </>
  );
}

interface FilterPanelProps {
  filters: ProductFilters;
  categories: Category[];
  totalCount: number;
  onChange: (key: string, value: string | null) => void;
}

// Hem masaüstündeki kenar panelinde hem mobildeki çekmecede kullanılır. Seçenekler radyo düğmesi değil
// aria-pressed'li butondur: aynı sayfada iki kopyası bulunduğunda radyo grupları birbirine karışırdı.
function FilterPanel({ filters, categories, totalCount, onChange }: FilterPanelProps) {
  return (
    <div className={styles.panel}>
      <section>
        <h2 className={styles.groupTitle}>Kategori</h2>
        <ul className={styles.options}>
          <li>
            <button
              type="button"
              className={styles.option}
              aria-pressed={!filters.category}
              onClick={() => onChange("category", null)}
            >
              Tümü
            </button>
          </li>
          {categories.map((category) => (
            <li key={category.name}>
              <button
                type="button"
                className={styles.option}
                aria-pressed={filters.category === category.name}
                onClick={() => onChange("category", category.name)}
              >
                {category.name}
                <span className={styles.optionCount}>{category.count}</span>
              </button>
            </li>
          ))}
        </ul>
      </section>

      <section>
        <h2 className={styles.groupTitle}>Fiyat</h2>
        <ul className={styles.options}>
          <li>
            <button
              type="button"
              className={styles.option}
              aria-pressed={!filters.priceRange}
              onClick={() => onChange("price", null)}
            >
              Tümü
            </button>
          </li>
          {PRICE_RANGES.map((range) => (
            <li key={range.value}>
              <button
                type="button"
                className={styles.option}
                aria-pressed={filters.priceRange === range.value}
                onClick={() => onChange("price", range.value)}
              >
                {range.label}
              </button>
            </li>
          ))}
        </ul>
      </section>

      <section>
        <h2 className={styles.groupTitle}>Diğer</h2>
        <label className={styles.check}>
          <input
            type="checkbox"
            checked={filters.inStockOnly}
            onChange={(e) => onChange("stock", e.target.checked ? "1" : null)}
          />
          Yalnızca stoktakiler
        </label>
        <label className={styles.check}>
          <input
            type="checkbox"
            checked={filters.onSaleOnly}
            onChange={(e) => onChange("sale", e.target.checked ? "1" : null)}
          />
          İndirimdekiler
        </label>
      </section>

      <p className={styles.panelNote}>Katalogda toplam {totalCount} ürün var.</p>
    </div>
  );
}
