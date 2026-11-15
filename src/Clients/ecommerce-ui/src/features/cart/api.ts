import apiClient from "../../lib/apiClient";
import type { CartItem } from "./types";

interface BasketItem {
  productId: string;
  productName: string;
  price: number;
  quantity: number;
  imageUrl: string | null;
}

interface CustomerBasket {
  userId: string;
  items: BasketItem[];
}

const toCartItem = (item: BasketItem): CartItem => ({
  id: item.productId,
  name: item.productName,
  price: item.price,
  quantity: item.quantity,
  imageUrl: item.imageUrl ?? "",
});

const toBasketItem = (item: CartItem): BasketItem => ({
  productId: item.id,
  productName: item.name,
  price: item.price,
  quantity: item.quantity,
  imageUrl: item.imageUrl,
});

// Sepeti olmayan kullanıcı için backend boş sepet döner (404 değil)
export async function getBasket() {
  const { data } = await apiClient.get<CustomerBasket>("/api/basket");
  return data.items.map(toCartItem);
}

export async function saveBasket(userId: string, items: CartItem[]) {
  const basket: CustomerBasket = { userId, items: items.map(toBasketItem) };
  await apiClient.post("/api/basket", basket);
}

export async function deleteBasket() {
  await apiClient.delete("/api/basket");
}
