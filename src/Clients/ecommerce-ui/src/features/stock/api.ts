import apiClient from "../../lib/apiClient";

interface ProductStock {
  productId: string;
  availableStock: number;
}

export async function getAvailableStock(productId: string) {
  const { data } = await apiClient.get<ProductStock>(`/api/stocks/${productId}`);
  return data.availableStock;
}

export async function getStockLevels() {
  const { data } = await apiClient.get<ProductStock[]>("/api/stocks");
  return new Map(data.map((s) => [s.productId, s.availableStock]));
}

export async function updateStock(productId: string, availableStock: number) {
  await apiClient.put(`/api/stocks/${productId}`, { availableStock });
}
