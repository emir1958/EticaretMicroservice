import { describe, expect, it } from "vitest";
import {
  UNCATEGORIZED,
  discountPercent,
  filterProducts,
  listCategories,
  normalizeText,
  sortProducts,
  stockStateOf,
  type ProductFilters,
} from "./catalogView";
import type { ProductWithStock } from "./types";

const product = (overrides: Partial<ProductWithStock> & { id: string }): ProductWithStock => ({
  name: `Ürün ${overrides.id}`,
  description: "",
  price: 1000,
  imageUrl: "",
  category: "Elektronik",
  compareAtPrice: null,
  isSample: false,
  availableStock: 10,
  ...overrides,
});

const NO_FILTERS: ProductFilters = { query: "", category: "", priceRange: "", inStockOnly: false, onSaleOnly: false };

describe("normalizeText", () => {
  it("Türkçe karakterleri ASCII karşılığına indirger", () => {
    expect(normalizeText("Işık")).toBe("isik");
    expect(normalizeText("ÇANTA")).toBe("canta");
    expect(normalizeText("İstanbul Şehir Öğretmen Ürün Göz")).toBe("istanbul sehir ogretmen urun goz");
  });
});

describe("discountPercent", () => {
  it("liste fiyatı satış fiyatından yüksekse yüzdeyi yuvarlayarak döner", () => {
    expect(discountPercent(product({ id: "1", price: 750, compareAtPrice: 1000 }))).toBe(25);
    expect(discountPercent(product({ id: "2", price: 666, compareAtPrice: 1000 }))).toBe(33);
  });

  it("liste fiyatı yoksa ya da satış fiyatından yüksek değilse indirim yoktur", () => {
    expect(discountPercent(product({ id: "1", compareAtPrice: null }))).toBeNull();
    expect(discountPercent(product({ id: "2", price: 1000, compareAtPrice: 1000 }))).toBeNull();
    expect(discountPercent(product({ id: "3", price: 1000, compareAtPrice: 900 }))).toBeNull();
  });
});

describe("stockStateOf", () => {
  it("0 ve altı tükendi, 5 ve altı azaldı, üstü stokta sayılır", () => {
    expect(stockStateOf(0)).toBe("out");
    expect(stockStateOf(-1)).toBe("out");
    expect(stockStateOf(1)).toBe("low");
    expect(stockStateOf(5)).toBe("low");
    expect(stockStateOf(6)).toBe("in");
  });
});

describe("listCategories", () => {
  it("kalabalık kategoriyi öne alır, 'Diğer' her zaman sonda kalır", () => {
    const categories = listCategories([
      product({ id: "1", category: "Giyim" }),
      product({ id: "2", category: "" }),
      product({ id: "3", category: "" }),
      product({ id: "4", category: "Elektronik" }),
      product({ id: "5", category: "Elektronik" }),
    ]);

    expect(categories.map((c) => [c.name, c.count])).toEqual([
      ["Elektronik", 2],
      ["Giyim", 1],
      [UNCATEGORIZED, 2],
    ]);
  });

  it("kategorinin görseli, görseli olan ilk üründen alınır", () => {
    const [category] = listCategories([
      product({ id: "1", imageUrl: "" }),
      product({ id: "2", imageUrl: "https://img/2.jpg" }),
    ]);

    expect(category.imageUrl).toBe("https://img/2.jpg");
  });
});

describe("filterProducts", () => {
  const products = [
    product({ id: "lamba", name: "Işıklı Lamba", category: "Ev", price: 500 }),
    product({ id: "klavye", name: "Mekanik Klavye", description: "RGB ışık", price: 2500, availableStock: 0 }),
    product({ id: "mouse", name: "Kablosuz Mouse", category: "", price: 1000, compareAtPrice: 1500 }),
  ];
  const ids = (list: ProductWithStock[]) => list.map((p) => p.id);

  it("Türkçe karaktersiz arama Türkçe karakterli ürünü bulur (isik → Işıklı, açıklamada ışık)", () => {
    expect(ids(filterProducts(products, { ...NO_FILTERS, query: "isik" }))).toEqual(["lamba", "klavye"]);
  });

  it("birden çok arama terimi birlikte sağlanmalıdır", () => {
    expect(ids(filterProducts(products, { ...NO_FILTERS, query: "isik lamba" }))).toEqual(["lamba"]);
  });

  it("kategorisiz ürün 'Diğer' filtresiyle bulunur", () => {
    expect(ids(filterProducts(products, { ...NO_FILTERS, category: UNCATEGORIZED }))).toEqual(["mouse"]);
  });

  it("yalnızca stokta olanlar ve yalnızca indirimdekiler filtreleri çalışır", () => {
    expect(ids(filterProducts(products, { ...NO_FILTERS, inStockOnly: true }))).toEqual(["lamba", "mouse"]);
    expect(ids(filterProducts(products, { ...NO_FILTERS, onSaleOnly: true }))).toEqual(["mouse"]);
  });

  it("fiyat aralığında alt sınır dahil, üst sınır hariçtir", () => {
    // "1000-2500": 1000 dahil, 2500 hariç
    expect(ids(filterProducts(products, { ...NO_FILTERS, priceRange: "1000-2500" }))).toEqual(["mouse"]);
    expect(ids(filterProducts(products, { ...NO_FILTERS, priceRange: "2500-5000" }))).toEqual(["klavye"]);
  });
});

describe("sortProducts", () => {
  const products = [
    product({ id: "pahali", name: "B", price: 3000 }),
    product({ id: "ucuz", name: "A", price: 100 }),
    product({ id: "indirimli", name: "C", price: 500, compareAtPrice: 1000 }),
  ];
  const ids = (list: ProductWithStock[]) => list.map((p) => p.id);

  it("fiyata göre artan ve azalan sıralar", () => {
    expect(ids(sortProducts(products, "price-asc"))).toEqual(["ucuz", "indirimli", "pahali"]);
    expect(ids(sortProducts(products, "price-desc"))).toEqual(["pahali", "indirimli", "ucuz"]);
  });

  it("indirim oranına göre en yüksek indirim başta", () => {
    expect(ids(sortProducts(products, "discount"))[0]).toBe("indirimli");
  });

  it("önerilen sırada stoktakiler, aralarında indirimliler önce gelir; tükenenler sona kalır", () => {
    const mixed = [
      product({ id: "tukenen-indirimli", availableStock: 0, price: 500, compareAtPrice: 1000 }),
      product({ id: "normal" }),
      product({ id: "indirimli", price: 500, compareAtPrice: 1000 }),
    ];

    expect(ids(sortProducts(mixed, "featured"))).toEqual(["indirimli", "normal", "tukenen-indirimli"]);
  });

  it("girdi dizisini değiştirmez", () => {
    const before = ids(products);
    sortProducts(products, "price-asc");
    expect(ids(products)).toEqual(before);
  });
});
