import type { CartItem } from "./types";

// Girişte misafir sepeti sunucudaki sepetle birleşir: aynı ürünün adetleri toplanır
export function mergeCarts(serverItems: CartItem[], guestItems: CartItem[]) {
  const merged = new Map(serverItems.map((item) => [item.id, item]));
  for (const guest of guestItems) {
    const existing = merged.get(guest.id);
    merged.set(guest.id, existing ? { ...existing, quantity: existing.quantity + guest.quantity } : guest);
  }
  return [...merged.values()];
}
