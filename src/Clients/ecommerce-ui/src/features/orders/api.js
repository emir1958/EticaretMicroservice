import apiClient from "../../lib/apiClient";

// Order.Api ile konuşan tek yer.

// idempotencyKey: aynı sipariş isteği (çift tıklama, ağ tekrarı) iki kez oluşturulmasın diye
// backend'deki [Idempotent] filtresine gönderilir. Dönen değer yeni siparişin id'si.
export async function createOrder({ address, paymentToken, items }, idempotencyKey) {
  const { data } = await apiClient.post(
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

// Müşteri: yalnızca token'daki kullanıcının siparişleri (backend başkasının siparişine 404 döner)
export async function getMyOrders() {
  const { data } = await apiClient.get("/api/orders/user");
  return data;
}

export async function getMyOrder(id) {
  const { data } = await apiClient.get(`/api/orders/user/${id}`);
  return data;
}

// --- Yönetim (admin) ---

export async function getAllOrders() {
  const { data } = await apiClient.get("/api/orders");
  return data;
}

export async function getOrder(id) {
  const { data } = await apiClient.get(`/api/orders/${id}`);
  return data;
}
