import type { Category, Product, ProductWithStock } from "./types";

// Katalog verisinden ekranın ihtiyaç duyduğu görünümleri türeten saf fonksiyonlar (HTTP yok, state yok).

export const UNCATEGORIZED = "Diğer";
export const LOW_STOCK_THRESHOLD = 5;

export const categoryOf = (product: Product) => product.category || UNCATEGORIZED;

export function listCategories(products: Product[]): Category[] {
  const byName = new Map<string, Category>();
  for (const product of products) {
    const name = categoryOf(product);
    const existing = byName.get(name);
    if (existing) {
      existing.count += 1;
      existing.imageUrl ||= product.imageUrl;
    } else {
      byName.set(name, { name, count: 1, imageUrl: product.imageUrl });
    }
  }

  // Kalabalık kategori önce; "Diğer" her zaman sonda
  return [...byName.values()].sort((a, b) => {
    if (a.name === UNCATEGORIZED) return 1;
    if (b.name === UNCATEGORIZED) return -1;
    return b.count - a.count || a.name.localeCompare(b.name, "tr");
  });
}

export function discountPercent(product: Product) {
  if (!product.compareAtPrice || product.compareAtPrice <= product.price) return null;
  return Math.round((1 - product.price / product.compareAtPrice) * 100);
}

export type StockState = "out" | "low" | "in";

export function stockStateOf(availableStock: number): StockState {
  if (availableStock <= 0) return "out";
  return availableStock <= LOW_STOCK_THRESHOLD ? "low" : "in";
}

// "isik" yazan "Işık"ı, "canta" yazan "Çanta"yı bulur
const TURKISH_FOLD: Record<string, string> = { ı: "i", ğ: "g", ü: "u", ş: "s", ö: "o", ç: "c" };

export const normalizeText = (text: string) =>
  text.toLocaleLowerCase("tr").replace(/[ığüşöç]/g, (char) => TURKISH_FOLD[char]);

export type SortKey = "featured" | "price-asc" | "price-desc" | "name" | "discount";

export const SORT_OPTIONS: { value: SortKey; label: string }[] = [
  { value: "featured", label: "Önerilen" },
  { value: "price-asc", label: "Fiyat: düşükten yükseğe" },
  { value: "price-desc", label: "Fiyat: yüksekten düşüğe" },
  { value: "discount", label: "İndirim oranı" },
  { value: "name", label: "İsim: A'dan Z'ye" },
];

export const PRICE_RANGES = [
  { value: "0-1000", label: "1.000 TL altı", min: 0, max: 1000 },
  { value: "1000-2500", label: "1.000 – 2.500 TL", min: 1000, max: 2500 },
  { value: "2500-5000", label: "2.500 – 5.000 TL", min: 2500, max: 5000 },
  { value: "5000-", label: "5.000 TL üzeri", min: 5000, max: Infinity },
];

export interface ProductFilters {
  query: string;
  category: string;
  priceRange: string;
  inStockOnly: boolean;
  onSaleOnly: boolean;
}

export function filterProducts(products: ProductWithStock[], filters: ProductFilters) {
  const terms = normalizeText(filters.query).split(/\s+/).filter(Boolean);
  const range = PRICE_RANGES.find((r) => r.value === filters.priceRange);

  return products.filter((product) => {
    if (filters.category && categoryOf(product) !== filters.category) return false;
    if (filters.inStockOnly && product.availableStock <= 0) return false;
    if (filters.onSaleOnly && discountPercent(product) === null) return false;
    if (range && (product.price < range.min || product.price >= range.max)) return false;
    if (terms.length > 0) {
      const haystack = normalizeText(`${product.name} ${product.category} ${product.description}`);
      if (!terms.every((term) => haystack.includes(term))) return false;
    }
    return true;
  });
}

// Önerilen sıra: stoktakiler önce, aralarında indirimli olanlar öne
function featuredRank(product: ProductWithStock) {
  return (product.availableStock > 0 ? 0 : 2) + (discountPercent(product) === null ? 1 : 0);
}

export function sortProducts(products: ProductWithStock[], sort: SortKey) {
  const sorted = [...products];
  switch (sort) {
    case "price-asc":
      return sorted.sort((a, b) => a.price - b.price);
    case "price-desc":
      return sorted.sort((a, b) => b.price - a.price);
    case "name":
      return sorted.sort((a, b) => a.name.localeCompare(b.name, "tr"));
    case "discount":
      return sorted.sort((a, b) => (discountPercent(b) ?? 0) - (discountPercent(a) ?? 0));
    default:
      return sorted.sort((a, b) => featuredRank(a) - featuredRank(b));
  }
}
