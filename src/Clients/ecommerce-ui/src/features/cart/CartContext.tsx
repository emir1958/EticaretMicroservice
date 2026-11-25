import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from "react";
import { useAuth } from "../auth/AuthContext";
import { useToast } from "../../shared/toast/ToastContext";
import * as basketApi from "./api";
import { mergeCarts } from "./mergeCarts";
import type { CartItem } from "./types";

type CartProduct = Omit<CartItem, "quantity">;

interface CartContextValue {
  cart: CartItem[];
  addToCart: (product: CartProduct, quantity: number, maxQuantity: number) => void;
  getCartQuantity: (id: string) => number;
  removeFromCart: (id: string) => void;
  updateQuantity: (id: string, quantity: number) => void;
  clearCart: () => Promise<void>;
  // Sipariş iptal olunca sepeti sipariş anındaki haline döndürür
  restoreCart: (items: CartItem[]) => void;
  totalAmount: number;
  isCartOpen: boolean;
  setIsCartOpen: (open: boolean) => void;
}

interface UserCart {
  userId: string | null;
  items: CartItem[];
}

const CartContext = createContext<CartContextValue | null>(null);

const GUEST_KEY = "ecommerce_cart_guest";
const userKey = (userId: string) => `ecommerce_cart_${userId}`;

function readStoredCart(key: string): CartItem[] {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(key) ?? "[]");
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function writeStoredCart(key: string, items: CartItem[]) {
  localStorage.setItem(key, JSON.stringify(items));
}

async function pushBasket(userId: string, items: CartItem[]) {
  try {
    await basketApi.saveBasket(userId, items);
  } catch (err) {
    console.error("Redis sepet senkronizasyon hatası:", err);
  }
}

export function CartProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const toast = useToast();
  const userId = user?.id;

  const [guestCart, setGuestCart] = useState(() => readStoredCart(GUEST_KEY));
  // A çıkıp B girerse B'nin sepeti yüklenene kadar A'nınki bir an bile gösterilmez
  const [userCart, setUserCart] = useState<UserCart>({ userId: null, items: [] });
  const [isCartOpen, setIsCartOpen] = useState(false);

  const isUserCartLoaded = Boolean(userId) && userCart.userId === userId;
  const cart = isUserCartLoaded ? userCart.items : guestCart;

  useEffect(() => {
    if (!userId) return;
    let ignore = false;

    const load = async () => {
      let serverItems: CartItem[];
      try {
        serverItems = await basketApi.getBasket();
      } catch (err) {
        console.warn("Redis'ten sepet çekilemedi, yerel kopya kullanılıyor:", err);
        serverItems = readStoredCart(userKey(userId));
      }

      if (ignore) return;

      const guestItems = readStoredCart(GUEST_KEY);
      const merged = mergeCarts(serverItems, guestItems);

      setUserCart({ userId, items: merged });
      writeStoredCart(userKey(userId), merged);

      if (guestItems.length > 0) {
        localStorage.removeItem(GUEST_KEY);
        setGuestCart([]);
        pushBasket(userId, merged);
      }
    };

    load();
    return () => {
      ignore = true;
    };
  }, [userId]);

  const commitCart = useCallback(
    (nextItems: CartItem[]) => {
      if (userId) {
        setUserCart({ userId, items: nextItems });
        writeStoredCart(userKey(userId), nextItems);
        pushBasket(userId, nextItems);
      } else {
        setGuestCart(nextItems);
        writeStoredCart(GUEST_KEY, nextItems);
      }
    },
    [userId],
  );

  const getCartQuantity = (id: string) => cart.find((item) => item.id === id)?.quantity ?? 0;

  // Sepetteki toplam adet stoğu aşmaz; asıl koruma sipariş anında Stock servisindeki rezervasyondur
  const addToCart = (product: CartProduct, quantity: number, maxQuantity: number) => {
    const { id, name, price, imageUrl } = product;
    const existing = cart.find((item) => item.id === id);
    const nextQuantity = Math.min((existing?.quantity ?? 0) + quantity, maxQuantity);
    if (nextQuantity <= (existing?.quantity ?? 0)) return;

    const nextItems = existing
      ? cart.map((item) => (item.id === id ? { ...item, name, price, imageUrl, quantity: nextQuantity } : item))
      : [...cart, { id, name, price, imageUrl, quantity: nextQuantity }];

    commitCart(nextItems);

    // Çekmece kendiliğinden açılmaz: art arda ürün ekleyen kullanıcının önü kesilmez
    toast.success(`${name} sepete eklendi.`, { label: "Sepeti gör", onClick: () => setIsCartOpen(true) });
  };

  const removeFromCart = (id: string) => {
    commitCart(cart.filter((item) => item.id !== id));
  };

  const updateQuantity = (id: string, quantity: number) => {
    if (quantity <= 0) {
      removeFromCart(id);
      return;
    }
    commitCart(cart.map((item) => (item.id === id ? { ...item, quantity } : item)));
  };

  const clearCart = async () => {
    if (!userId) {
      setGuestCart([]);
      localStorage.removeItem(GUEST_KEY);
      return;
    }

    setUserCart({ userId, items: [] });
    localStorage.removeItem(userKey(userId));
    try {
      await basketApi.deleteBasket();
    } catch (err) {
      console.error("Redis sepet silme hatası:", err);
    }
  };

  const restoreCart = commitCart;

  const totalAmount = cart.reduce((sum, item) => sum + item.price * item.quantity, 0);

  return (
    <CartContext
      value={{
        cart,
        addToCart,
        getCartQuantity,
        removeFromCart,
        updateQuantity,
        clearCart,
        restoreCart,
        totalAmount,
        isCartOpen,
        setIsCartOpen,
      }}
    >
      {children}
    </CartContext>
  );
}

export function useCart() {
  const context = useContext(CartContext);
  if (!context) throw new Error("useCart, CartProvider içinde kullanılmalı.");
  return context;
}
