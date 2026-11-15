import apiClient from "../../lib/apiClient";
import type { NewProductInput, Product, ProductInput, ProductWithStock } from "./types";

interface ProductStock {
  productId: string;
  availableStock: number;
}

export async function getProducts() {
  const { data } = await apiClient.get<Product[]>("/api/products");
  return data;
}

export async function getProduct(id: string) {
  const { data } = await apiClient.get<Product>(`/api/products/${id}`);
  return data;
}

export async function getAvailableStock(productId: string) {
  const { data } = await apiClient.get<ProductStock>(`/api/stocks/${productId}`);
  return data.availableStock;
}

// Stock servisi yanıt vermezse ürün listesi yine gösterilir (stoklar 0 görünür)
async function getStockMap() {
  try {
    const { data } = await apiClient.get<ProductStock[]>("/api/stocks");
    return new Map(data.map((s) => [s.productId, s.availableStock]));
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

export async function updateStock(productId: string, availableStock: number) {
  await apiClient.put(`/api/stocks/${productId}`, { availableStock });
}

export async function deleteProduct(id: string) {
  await apiClient.delete(`/api/products/${id}`);
}
