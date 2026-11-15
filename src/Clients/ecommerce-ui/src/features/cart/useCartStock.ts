import { useAsync } from "../../shared/hooks/useAsync";
import { getStockLevels } from "../stock/api";
import { useCart } from "./CartContext";

// Stok, sepete eklendiği anki değer değil, her açılışta güncel değer. Servis yanıt vermezse
// sınır uygulanmaz (stockFor undefined döner); sipariş anında Stock servisi yine korur.
export function useCartStock(enabled = true) {
  const { cart } = useCart();
  const { data: stockLevels } = useAsync(() => (enabled ? getStockLevels() : Promise.resolve(null)), [enabled]);

  const stockFor = (id: string) => (stockLevels ? (stockLevels.get(id) ?? 0) : undefined);

  const hasStockIssue = cart.some((item) => {
    const stock = stockFor(item.id);
    return stock !== undefined && item.quantity > stock;
  });

  return { stockFor, hasStockIssue };
}
