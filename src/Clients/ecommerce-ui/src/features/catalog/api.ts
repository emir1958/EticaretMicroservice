import apiClient from "../../lib/apiClient";
import { getStockLevels } from "../stock/api";
import { listCategories } from "./catalogView";
import type { NewProductInput, Product, ProductInput, ProductWithStock } from "./types";

// Ürün listesini aynı anda birden çok yer ister (header'daki kategoriler + sayfanın kendisi).
// Kısa ömürlü bu önbellek aynı isteğin iki kez gitmesini önler; ürün değişince temizlenir.
// Stok burada tutulmaz: her seferinde Stock servisinden taze okunur.
const PRODUCTS_TTL_MS = 30_000;
let productsCache: { promise: Promise<Product[]>; fetchedAt: number } | null = null;

export function getProducts() {
  if (productsCache && Date.now() - productsCache.fetchedAt < PRODUCTS_TTL_MS) {
    return productsCache.promise;
  }

  const promise = apiClient.get<Product[]>("/api/products").then(({ data }) => data);
  productsCache = { promise, fetchedAt: Date.now() };
  promise.catch(() => {
    if (productsCache?.promise === promise) productsCache = null;
  });
  return promise;
}

export async function getProduct(id: string) {
  const { data } = await apiClient.get<Product>(`/api/products/${id}`);
  return data;
}

export async function getCategories() {
  return listCategories(await getProducts());
}

// Stock servisi yanıt vermezse ürün listesi yine gösterilir (stoklar 0 görünür)
async function getStockMap() {
  try {
    return await getStockLevels();
  } catch (err) {
    console.warn("Stok servisi yanıt vermedi:", err);
    return new Map<string, number>();
  }
}

export async function getProductsWithStock(): Promise<ProductWithStock[]> {
  const [products, stockMap] = await Promise.all([getProducts(), getStockMap()]);
  return products.map((p) => ({ ...p, availableStock: stockMap.get(p.id) ?? 0 }));
}

export async function createProduct(product: NewProductInput) {
  await apiClient.post("/api/products", product);
  productsCache = null;
}

// stock de gönderilir: Catalog her güncellemede Stock servisine StockUpdatedEvent yayınlar.
// Gönderilmezse event stoğu 0 olarak taşır ve stok bir an için (ya da kalıcı olarak) sıfırlanabilir.
export async function updateProduct(id: string, product: ProductInput, stock: number) {
  await apiClient.put(`/api/products/${id}`, { ...product, stock });
  productsCache = null;
}

export async function deleteProduct(id: string) {
  await apiClient.delete(`/api/products/${id}`);
  productsCache = null;
}
