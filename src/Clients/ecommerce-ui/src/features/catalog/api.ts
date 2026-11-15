import apiClient from "../../lib/apiClient";

// Catalog.Api ve Stock.Api ile konuşan tek yer.
// Ürün: { id, name, description, price, imageUrl }   Stok: { productId, availableStock }
// Sayfalar burada dönen nesnelere güvenir; cevabın şeklini bilmek bu dosyanın işi.

export async function getProducts() {
  const { data } = await apiClient.get("/api/products");
  return data;
}

export async function getProduct(id) {
  const { data } = await apiClient.get(`/api/products/${id}`);
  return data;
}

export async function getAvailableStock(productId) {
  const { data } = await apiClient.get(`/api/stocks/${productId}`);
  return data.availableStock ?? 0;
}

// Stock servisi yanıt vermezse ürün listesi yine gösterilir (stoklar 0 görünür):
// bir servisin çökmesi tüm vitrini düşürmesin.
async function getStockMap() {
  try {
    const { data } = await apiClient.get("/api/stocks");
    return new Map(data.map((s) => [s.productId, s.availableStock]));
  } catch (err) {
    console.warn("Stok servisi yanıt vermedi:", err);
    return new Map();
  }
}

// Ürünler Catalog'dan, stoklar Stock servisinden gelir; ekranda ikisi birlikte gerekir.
export async function getProductsWithStock() {
  const [products, stockMap] = await Promise.all([getProducts(), getStockMap()]);
  return products.map((p) => ({ ...p, availableStock: stockMap.get(p.id) ?? 0 }));
}

// --- Yönetim (admin) işlemleri ---

// Yeni ürünün başlangıç stoğu Catalog'dan event ile Stock servisine iletilir.
export async function createProduct({ name, description, price, imageUrl, initialStock }) {
  await apiClient.post("/api/products", { name, description, price, imageUrl, initialStock });
}

export async function updateProduct(id, { name, description, price, imageUrl }) {
  await apiClient.put(`/api/products/${id}`, { name, description, price, imageUrl });
}

export async function updateStock(productId, availableStock) {
  await apiClient.put(`/api/stocks/${productId}`, { availableStock });
}

export async function deleteProduct(id) {
  await apiClient.delete(`/api/products/${id}`);
}
