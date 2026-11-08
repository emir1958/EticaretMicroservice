import { createContext, useContext, useState, useEffect } from "react";
import { useAuth } from "../auth/AuthContext";
import * as basketApi from "./api";

const CartContext = createContext();

// ---------------------------------------------------------------------------
// Saklama (localStorage) yardımcıları: React'ten bağımsız, saf fonksiyonlar.
// Sunucu ↔ UI dönüşümü ./api.js'te.
// ---------------------------------------------------------------------------
const GUEST_KEY = "ecommerce_cart_guest";
const userKey = (userId) => `ecommerce_cart_${userId}`;

function readStoredCart(key) {
  try {
    const parsed = JSON.parse(localStorage.getItem(key) ?? "[]");
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return []; // Bozuk JSON uygulamayı çökertmesin
  }
}

function writeStoredCart(key, items) {
  localStorage.setItem(key, JSON.stringify(items));
}

// Girişte misafir sepeti kullanıcının sunucudaki sepetiyle birleşir; aynı ürünün adetleri toplanır.
function mergeCarts(serverItems, guestItems) {
  const merged = new Map(serverItems.map((item) => [item.id, item]));
  for (const guest of guestItems) {
    const existing = merged.get(guest.id);
    merged.set(guest.id, existing ? { ...existing, quantity: existing.quantity + guest.quantity } : guest);
  }
  return [...merged.values()];
}

async function pushBasket(userId, items) {
  try {
    await basketApi.saveBasket(userId, items);
  } catch (err) {
    console.error("Redis sepet senkronizasyon hatası:", err);
  }
}

// ---------------------------------------------------------------------------
// Provider
// ---------------------------------------------------------------------------
export const CartProvider = ({ children }) => {
  const { user } = useAuth();
  const userId = user?.id;

  const [guestCart, setGuestCart] = useState(() => readStoredCart(GUEST_KEY));
  // Kullanıcı sepeti hangi kullanıcıya ait olduğuyla birlikte tutulur: A çıkıp B girerse
  // B'nin sepeti yüklenene kadar A'nınki bir an bile gösterilmez.
  const [userCart, setUserCart] = useState({ userId: null, items: [] });
  const [isCartOpen, setIsCartOpen] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");

  // Gösterilen sepet state değil, türetilen değer. Çıkışta ayrıca "sıfırlama" gerekmez:
  // userId yoksa misafir sepeti görünür. Girişten sonra sunucu sepeti gelene kadar da misafir sepeti görünür.
  const isUserCartLoaded = Boolean(userId) && userCart.userId === userId;
  const cart = isUserCartLoaded ? userCart.items : guestCart;

  // Kullanıcı giriş yaptığında (veya sayfa giriş yapılmış hâlde açıldığında) sunucu sepetini yükle
  // ve misafir sepetini içine kat. Effect'in işi tam olarak bu: dış sistemle (Basket API) senkronizasyon.
  useEffect(() => {
    if (!userId) return;
    let ignore = false;

    const load = async () => {
      let serverItems;
      try {
        serverItems = await basketApi.getBasket();
      } catch (err) {
        console.warn("Redis'ten sepet çekilemedi, yerel kopya kullanılıyor:", err);
        serverItems = readStoredCart(userKey(userId));
      }
      // Cevap gelene kadar kullanıcı değiştiyse (veya StrictMode effect'i iki kez çalıştırdıysa) sonucu at.
      // Kontrol, misafir sepetine dokunmadan önce yapılır; yoksa merge iki kez uygulanırdı.
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

  // Tüm değişiklikler buradan geçer: önce yeni sepet saf olarak hesaplanır, sonra state'e
  // ve saklama yerine yazılır. Yan etki (HTTP) setState updater'ının içinde değil, burada.
  const commitCart = (nextItems) => {
    if (userId) {
      setUserCart({ userId, items: nextItems });
      writeStoredCart(userKey(userId), nextItems);
      pushBasket(userId, nextItems);
    } else {
      setGuestCart(nextItems);
      writeStoredCart(GUEST_KEY, nextItems);
    }
  };

  const addToCart = (product, addQty = 1) => {
    const targetId = product.id || product._id;
    if (!targetId) return;

    const existing = cart.find((item) => item.id === targetId);
    const nextItems = existing
      ? cart.map((item) =>
          item.id === targetId
            ? {
                ...item,
                name: product.name || item.name,
                price: product.price ?? item.price,
                imageUrl: product.imageUrl || item.imageUrl,
                quantity: item.quantity + addQty,
              }
            : item,
        )
      : [
          ...cart,
          {
            id: targetId,
            name: product.name || "Ürün",
            price: product.price || 0,
            imageUrl: product.imageUrl || "",
            quantity: addQty,
          },
        ];

    commitCart(nextItems);

    setSuccessMessage("Ürün sepetinize başarıyla eklendi.");
    setTimeout(() => {
      setSuccessMessage("");
      setIsCartOpen(true);
    }, 800);
  };

  const removeFromCart = (id) => {
    commitCart(cart.filter((item) => item.id !== id));
  };

  const updateQuantity = (id, quantity) => {
    if (quantity <= 0) {
      removeFromCart(id);
      return;
    }
    commitCart(cart.map((item) => (item.id === id ? { ...item, quantity } : item)));
  };

  // Sipariş verildikten sonra çağrılır. Sunucuda boş sepet yazmak yerine sepet silinir.
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

  const totalAmount = cart.reduce((sum, item) => sum + (item.price || 0) * (item.quantity || 1), 0);

  return (
    <CartContext.Provider
      value={{
        cart,
        addToCart,
        removeFromCart,
        updateQuantity,
        clearCart,
        totalAmount,
        isCartOpen,
        setIsCartOpen,
        successMessage,
      }}
    >
      {children}
    </CartContext.Provider>
  );
};

// Bkz. AuthContext.jsx: hook provider ile bilinçli olarak aynı dosyada.
// eslint-disable-next-line react-refresh/only-export-components
export const useCart = () => useContext(CartContext);
