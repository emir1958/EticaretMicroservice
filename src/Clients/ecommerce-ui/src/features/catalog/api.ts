import apiClient from "../../lib/apiClient";
import { getStockLevels } from "../stock/api";
import type { NewProductInput, Product, ProductInput, ProductWithStock } from "./types";

export async function getProducts() {
  const { data } = await apiClient.get<Product[]>("/api/products");
  return data;
}

export async function getProduct(id: string) {
  const { data } = await apiClient.get<Product>(`/api/products/${id}`);
  return data;
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
}

export async function updateProduct(id: string, product: ProductInput) {
  await apiClient.put(`/api/products/${id}`, product);
}

export async function deleteProduct(id: string) {
  await apiClient.delete(`/api/products/${id}`);
}
