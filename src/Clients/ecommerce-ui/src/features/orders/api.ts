import apiClient from "../../lib/apiClient";
import type { NewOrder, Order } from "./types";

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

export async function getAllOrders() {
  const { data } = await apiClient.get<Order[]>("/api/orders");
  return data;
}

export async function getOrder(id: number) {
  const { data } = await apiClient.get<Order>(`/api/orders/${id}`);
  return data;
}
