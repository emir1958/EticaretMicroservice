import apiClient from "../../lib/apiClient";

export interface ProductStock {
  productId: string;
  // Satılabilir adet
  availableStock: number;
  // Ödemesi beklenen siparişler için ayrılmış adet
  reservedStock: number;
}

export async function getAvailableStock(productId: string) {
  const { data } = await apiClient.get<ProductStock>(`/api/stocks/${productId}`);
  return data.availableStock;
}

export async function getStocks() {
  const { data } = await apiClient.get<ProductStock[]>("/api/stocks");
  return data;
}

export async function getStockLevels() {
  const stocks = await getStocks();
  return new Map(stocks.map((s) => [s.productId, s.availableStock]));
}

export async function updateStock(productId: string, availableStock: number) {
  await apiClient.put(`/api/stocks/${productId}`, { availableStock });
}
