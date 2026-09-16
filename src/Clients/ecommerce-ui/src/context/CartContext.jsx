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

  useEffect(() => {
    localStorage.setItem("ecommerce_cart", JSON.stringify(cart));
  }, [cart]);

  const addToCart = (product) => {
    const targetId = product.id || product._id;
    if (!targetId) return;

    setCart((prev) => {
      const existing = prev.find((item) => item.id === targetId);
      if (existing) {
        // 🟢 İsim veya fiyat değiştiyse onları da güncelle ve adedi artır
        return prev.map((item) =>
          item.id === targetId
            ? {
                ...item,
                name: product.name || item.name,
                price: product.price ?? item.price,
                imageUrl: product.imageUrl || item.imageUrl,
                quantity: item.quantity + 1,
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
          quantity: 1,
        },
      ];
    });
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
      }}
    >
      {children}
    </CartContext.Provider>
  );
};

export const useCart = () => useContext(CartContext);
