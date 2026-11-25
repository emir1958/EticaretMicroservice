import apiClient from "../../lib/apiClient";
import type {
  CustomerOrderStats,
  CustomerSummary,
  DailySales,
  NewOrder,
  Order,
  OrderSearchParams,
  PagedOrders,
  ProductSales,
  SalesSummary,
} from "./types";

// Aynı istek iki kez gelirse (çift tıklama, ağ tekrarı) backend'deki [Idempotent] filtresi ikinci siparişi engeller
export async function createOrder({ address, paymentToken, items }: NewOrder, idempotencyKey: string) {
  const { data } = await apiClient.post<{ orderId: number }>(
    "/api/orders",
    {
      address,
      payment: { paymentToken },
      orderItems: items.map((item) => ({
        productId: item.id,
        productName: item.name,
        price: item.price,
        quantity: item.quantity,
      })),
    },
    { headers: { "Idempotency-Key": idempotencyKey } },
  );
  return data.orderId;
}

export async function getMyOrders() {
  const { data } = await apiClient.get<Order[]>("/api/orders/user");
  return data;
}

export async function getMyOrder(id: number) {
  const { data } = await apiClient.get<Order>(`/api/orders/user/${id}`);
  return data;
}


export async function getOrder(id: number) {
  const { data } = await apiClient.get<Order>(`/api/orders/${id}`);
  return data;
}

// Admin sipariş listesi: sayfalama, filtre ve arama sunucuda yapılır
export async function searchOrders({ q, status, buyerId, page = 1, pageSize = 15 }: OrderSearchParams = {}) {
  const { data } = await apiClient.get<PagedOrders>("/api/orders", {
    params: { q: q || undefined, status, buyerId, page, pageSize },
  });
  return data;
}

// Raporlar: dönem [from, to) olarak kesin birer an (ISO) gönderilir; gün sınırını istemci belirler (yerel gece yarısı)
const periodParams = ({ from, to }: { from: Date; to: Date }) => ({ from: from.toISOString(), to: to.toISOString() });

export async function getSalesSummary(period: { from: Date; to: Date }) {
  const { data } = await apiClient.get<SalesSummary>("/api/orders/stats/summary", { params: periodParams(period) });
  return data;
}

export async function getDailySales(period: { from: Date; to: Date }) {
  const { data } = await apiClient.get<DailySales[]>("/api/orders/stats/daily", { params: periodParams(period) });
  return data;
}

export async function getProductSales(period: { from: Date; to: Date }, limit?: number) {
  const { data } = await apiClient.get<ProductSales[]>("/api/orders/stats/products", {
    params: { ...periodParams(period), limit },
  });
  return data;
}

// buyerIds verilmezse tüm alıcılar döner
export async function getCustomerStats(buyerIds?: string[]) {
  const { data } = await apiClient.get<CustomerOrderStats[]>("/api/orders/stats/customers", {
    params: { buyerIds },
    paramsSerializer: { indexes: null },
  });
  return data;
}

export async function getCustomerSummary() {
  const { data } = await apiClient.get<CustomerSummary>("/api/orders/stats/customers/summary");
  return data;
}
