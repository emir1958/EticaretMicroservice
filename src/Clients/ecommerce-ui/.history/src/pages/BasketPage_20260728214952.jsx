import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import api from "../services/api";

export default function BasketPage() {
  const [basket, setBasket] = useState({ items: [] });
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  // Sepet Bilgisini Çek
  const fetchBasket = () => {
    api
      .get("/basket")
      .then((res) => {
        if (res.data) setBasket(res.data);
        setLoading(false);
      })
      .catch((err) => {
        console.error("Sepet çekilemedi:", err);
        setLoading(false);
      });
  };

  useEffect(() => {
    fetchBasket();
  }, []);

  // Adet Güncelleme (Artır / Azalt)
  const handleQuantityChange = (productId, change) => {
    const updatedItems = basket.items
      .map((item) => {
        if (item.productId === productId) {
          const newQty = item.quantity + change;
          return newQty > 0 ? { ...item, quantity: newQty } : null;
        }
        return item;
      })
      .filter(Boolean); // Adedi 0'a düşenleri listeden temizler

    updateBasketOnApi(updatedItems);
  };

  // Tekil Ürün Silme
  const handleRemoveItem = (productId) => {
    const updatedItems = basket.items.filter(
      (item) => item.productId !== productId,
    );
    updateBasketOnApi(updatedItems);
  };

  // Sepeti Tamamen Temizleme
  const handleClearBasket = () => {
    if (!window.confirm("Sepetinizi temizlemek istediğinize emin misiniz?"))
      return;

    api
      .delete("/basket")
      .then(() => {
        setBasket({ items: [] });
      })
      .catch((err) => console.error("Sepet silinemedi:", err));
  };

  // API'ye Güncel Sepeti Gönderme Helper'ı
  const updateBasketOnApi = (items) => {
    const updatedBasket = { ...basket, items };
    api
      .post("/basket", updatedBasket)
      .then((res) => setBasket(res.data))
      .catch((err) => console.error("Sepet güncellenemedi:", err));
  };

  // Toplam Tutar Hesaplama
  const totalPrice =
    basket?.items?.reduce((acc, item) => acc + item.price * item.quantity, 0) ||
    0;

  if (loading)
    return <div style={{ padding: "30px" }}>🛒 Sepetiniz yükleniyor...</div>;

  return (
    <div
      style={{
        padding: "30px",
        fontFamily: "Arial, sans-serif",
        maxWidth: "800px",
        margin: "0 auto",
      }}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
        }}
      >
        <h2>🛒 Alışveriş Sepetim</h2>
        <Link
          to="/"
          style={{
            textDecoration: "none",
            color: "#2980b9",
            fontWeight: "bold",
          }}
        >
          ← Ürün Kataloğuna Dön
        </Link>
      </div>

      {basket.items.length === 0 ? (
        <div
          style={{ textAlign: "center", marginTop: "50px", color: "#7f8c8d" }}
        >
          <h3>Sepetiniz henüz boş!</h3>
          <p>Alışverişe başlamak için ürün kataloğuna göz atabilirsiniz.</p>
        </div>
      ) : (
        <div>
          <div
            style={{
              marginTop: "20px",
              border: "1px solid #e0e0e0",
              borderRadius: "8px",
              overflow: "hidden",
            }}
          >
            {basket.items.map((item) => (
              <div
                key={item.productId}
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  padding: "15px 20px",
                  borderBottom: "1px solid #eee",
                  backgroundColor: "#fff",
                }}
              >
                <div>
                  <h4 style={{ margin: "0 0 5px 0", color: "#2c3e50" }}>
                    {item.productName}
                  </h4>
                  <span style={{ color: "#27ae60", fontWeight: "bold" }}>
                    {item.price} TL
                  </span>
                </div>

                {/* Adet Arttır / Azalt Butonları */}
                <div
                  style={{ display: "flex", alignItems: "center", gap: "10px" }}
                >
                  <button
                    onClick={() => handleQuantityChange(item.productId, -1)}
                    style={{
                      padding: "5px 10px",
                      cursor: "pointer",
                      borderRadius: "4px",
                      border: "1px solid #ccc",
                    }}
                  >
                    -
                  </button>
                  <span
                    style={{
                      fontWeight: "bold",
                      width: "20px",
                      textAlign: "center",
                    }}
                  >
                    {item.quantity}
                  </span>
                  <button
                    onClick={() => handleQuantityChange(item.productId, 1)}
                    style={{
                      padding: "5px 10px",
                      cursor: "pointer",
                      borderRadius: "4px",
                      border: "1px solid #ccc",
                    }}
                  >
                    +
                  </button>
                </div>

                <div
                  style={{ display: "flex", alignItems: "center", gap: "20px" }}
                >
                  <span style={{ fontWeight: "bold", fontSize: "1.1em" }}>
                    {item.price * item.quantity} TL
                  </span>
                  <button
                    onClick={() => handleRemoveItem(item.productId)}
                    style={{
                      backgroundColor: "#e74c3c",
                      color: "#fff",
                      border: "none",
                      padding: "6px 12px",
                      borderRadius: "4px",
                      cursor: "pointer",
                    }}
                  >
                    Sil
                  </button>
                </div>
              </div>
            ))}
          </div>

          {/* Sepet Özeti ve İşlemler */}
          <div
            style={{
              marginTop: "20px",
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              backgroundColor: "#f8f9fa",
              padding: "20px",
              borderRadius: "8px",
            }}
          >
            <button
              onClick={handleClearBasket}
              style={{
                backgroundColor: "#7f8c8d",
                color: "#fff",
                border: "none",
                padding: "10px 15px",
                borderRadius: "6px",
                cursor: "pointer",
              }}
            >
              Sepeti Temizle
            </button>

            <div>
              <span style={{ fontSize: "1.2em", marginRight: "15px" }}>
                Toplam Tutar:{" "}
                <b style={{ color: "#27ae60" }}>{totalPrice} TL</b>
              </span>
              <button
                onClick={() => alert("Sipariş tamamlama adımına geçiliyor...")}
                style={{
                  backgroundColor: "#27ae60",
                  color: "#fff",
                  border: "none",
                  padding: "12px 20px",
                  borderRadius: "6px",
                  fontWeight: "bold",
                  cursor: "pointer",
                }}
              >
                Siparişi Tamamla 🚀
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
