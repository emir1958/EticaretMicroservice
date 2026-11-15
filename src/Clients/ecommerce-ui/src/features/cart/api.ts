import apiClient from "../../lib/apiClient";

// Basket.Api (Redis) ile konuşan tek yer.
// Sunucu: { userId, items: [{ productId, productName, price, quantity, imageUrl }] }
// UI:     [{ id, name, price, quantity, imageUrl }]
// Dönüşüm burada yapılır; CartContext yalnızca UI şeklini bilir.

const toCartItem = (item) => ({
  id: item.productId,
  name: item.productName,
  price: item.price,
  quantity: item.quantity,
  imageUrl: item.imageUrl ?? "",
});

const toBasketItem = (item) => ({
  productId: item.id,
  productName: item.name,
  price: item.price,
  quantity: item.quantity,
  imageUrl: item.imageUrl,
});

// Sepeti olmayan kullanıcı için backend boş sepet döner (404 değil)
export async function getBasket() {
  const { data } = await apiClient.get("/api/basket");
  return data.items.map(toCartItem);
}

export async function saveBasket(userId, items) {
  await apiClient.post("/api/basket", { userId, items: items.map(toBasketItem) });
}

export async function deleteBasket() {
  await apiClient.delete("/api/basket");
}
