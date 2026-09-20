import React, { createContext, useContext, useState, useEffect } from "react";

const CartContext = createContext();

export const CartProvider = ({ children }) => {
  const [cart, setCart] = useState(() => {
    try {
      const savedCart = localStorage.getItem("ecommerce_cart");
      return savedCart ? JSON.parse(savedCart) : [];
    } catch {
      return [];
    }
  });

  // 🟢 Yeni: Popup ve Bildirim State'leri
  const [isCartOpen, setIsCartOpen] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    localStorage.setItem("ecommerce_cart", JSON.stringify(cart));
  }, [cart]);

  // 🟢 Güncellendi: addQty parametresi eklendi
  const addToCart = (product, addQty = 1) => {
    const targetId = product.id || product._id;
    if (!targetId) return;

    setCart((prev) => {
      const existing = prev.find((item) => item.id === targetId);
      if (existing) {
        return prev.map((item) =>
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
      }
      return [
        ...prev,
        {
          id: targetId,
          name: product.name || "Ürün",
          price: product.price || 0,
          imageUrl: product.imageUrl || "",
          quantity: addQty,
        },
      ];
    });

    // 🟢 Ekleme başarılı bildirimi ve Popup'ı açma
    setSuccessMessage("Ürün sepetinize başarıyla eklendi.");
    setTimeout(() => {
      setSuccessMessage("");
      setIsCartOpen(true);
    }, 800); // 800ms sonra sepet açılsın
  };

  const removeFromCart = (id) => {
    setCart((prev) => prev.filter((item) => item.id !== id));
  };

  const updateQuantity = (id, quantity) => {
    if (quantity <= 0) {
      removeFromCart(id);
      return;
    }
    setCart((prev) =>
      prev.map((item) => (item.id === id ? { ...item, quantity } : item)),
    );
  };

  const clearCart = () => {
    setCart([]);
    localStorage.removeItem("ecommerce_cart");
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
        isCartOpen, // Eklendi
        setIsCartOpen, // Eklendi
        successMessage, // Eklendi
      }}
    >
      {children}
    </CartContext.Provider>
  );
};

export const useCart = () => useContext(CartContext);
