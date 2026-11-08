import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useRef,
} from "react";
import api from "../services/api";
import { useAuth } from "./AuthContext";

const CartContext = createContext();

export const CartProvider = ({ children }) => {
  const { user, isAuthenticated } = useAuth();
  const userId = user?.id;

  const [cart, setCart] = useState([]);
  const [isCartOpen, setIsCartOpen] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const isInitialMount = useRef(true);

  // 🟢 1. KULLANICI DEĞİŞTİĞİNDE VEYA ÇIKIŞ YAPILDIĞINDA SEPETİ YÖNETME
  useEffect(() => {
    const loadBasket = async () => {
      if (isAuthenticated && userId) {
        try {
          // Redis'ten kullanıcının kendi sepetini çekiyoruz
          const res = await api.get("/api/Basket");
          const backendItems = res.data?.items || res.data?.Items || [];

          const mappedCart = backendItems.map((item) => ({
            id: item.productId || item.ProductId || item.id,
            name: item.productName || item.ProductName || item.name,
            price:
              item.price ?? item.Price ?? item.unitPrice ?? item.UnitPrice ?? 0,
            quantity: item.quantity || item.Quantity || 1,
            imageUrl:
              item.imageUrl ||
              item.ImageUrl ||
              item.pictureUrl ||
              item.PictureUrl ||
              "",
          }));

          setCart(mappedCart);
          localStorage.setItem(
            `ecommerce_cart_${userId}`,
            JSON.stringify(mappedCart),
          );
        } catch (err) {
          console.warn(
            "Redis'ten sepet çekilemedi, yerel hafıza kontrol ediliyor:",
            err,
          );
          const cached = localStorage.getItem(`ecommerce_cart_${userId}`);
          setCart(cached ? JSON.parse(cached) : []);
        }
      } else {
        // Kullanıcı giriş yapmamışsa (Misafir modu)
        const guestCart = localStorage.getItem("ecommerce_cart_guest");
        setCart(guestCart ? JSON.parse(guestCart) : []);
      }
    };

    loadBasket();
  }, [userId, isAuthenticated]);

  // 🟢 2. REDIS VE LOCALSTORAGE'A SENKRONİZASYON YARDIMCISI
  const syncBasket = async (updatedCart) => {
    const storageKey = userId
      ? `ecommerce_cart_${userId}`
      : "ecommerce_cart_guest";
    localStorage.setItem(storageKey, JSON.stringify(updatedCart));

    // Kullanıcı giriş yapmışsa Redis'e (Basket.Api) gönder
    if (isAuthenticated && userId) {
      try {
        await api.post("/api/Basket", {
          userId: userId,
          items: updatedCart.map((i) => ({
            productId: i.id,
            productName: i.name,
            price: i.price,
            unitPrice: i.price,
            quantity: i.quantity,
            imageUrl: i.imageUrl,
            pictureUrl: i.imageUrl,
          })),
        });
      } catch (err) {
        console.error("Redis sepet senkronizasyon hatası:", err);
      }
    }
  };

  // 🟢 3. SEPETE EKLEME
  const addToCart = (product, addQty = 1) => {
    const targetId = product.id || product._id;
    if (!targetId) return;

    setCart((prev) => {
      let updated;
      const existing = prev.find((item) => item.id === targetId);

      if (existing) {
        updated = prev.map((item) =>
          item.id === targetId
            ? {
                ...item,
                name: product.name || item.name,
                price: product.price ?? item.price,
                imageUrl: product.imageUrl || item.imageUrl,
                quantity: item.quantity + addQty,
              }
            : item,
        );
      } else {
        updated = [
          ...prev,
          {
            id: targetId,
            name: product.name || "Ürün",
            price: product.price || 0,
            imageUrl: product.imageUrl || "",
            quantity: addQty,
          },
        ];
      }

      syncBasket(updated);
      return updated;
    });

    setSuccessMessage("Ürün sepetinize başarıyla eklendi.");
    setTimeout(() => {
      setSuccessMessage("");
      setIsCartOpen(true);
    }, 800);
  };

  // 🟢 4. SEPETTEN ÇIKARMA
  const removeFromCart = (id) => {
    setCart((prev) => {
      const updated = prev.filter((item) => item.id !== id);
      syncBasket(updated);
      return updated;
    });
  };

  // 🟢 5. MİKTAR GÜNCELLEME
  const updateQuantity = (id, quantity) => {
    if (quantity <= 0) {
      removeFromCart(id);
      return;
    }

    setCart((prev) => {
      const updated = prev.map((item) =>
        item.id === id ? { ...item, quantity } : item,
      );
      syncBasket(updated);
      return updated;
    });
  };

  // 🟢 6. SEPETİ TEMİZLEME
  const clearCart = async () => {
    setCart([]);
    const storageKey = userId
      ? `ecommerce_cart_${userId}`
      : "ecommerce_cart_guest";
    localStorage.removeItem(storageKey);

    if (isAuthenticated && userId) {
      try {
        await api.delete("/api/Basket");
      } catch (err) {
        console.error("Redis sepet silme hatası:", err);
      }
    }
  };

  const totalAmount = cart.reduce(
    (sum, item) => sum + (item.price || 0) * (item.quantity || 1),
    0,
  );

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

export const useCart = () => useContext(CartContext);
