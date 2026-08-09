import { useEffect, useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import api from "../services/api";
import {
  logout,
  getCurrentToken,
  getCurrentUser,
} from "../services/authService";

export default function ProductsPage() {
  const [products, setProducts] = useState([]);
  const [basket, setBasket] = useState({ items: [] }); // 🛒 Sepet State'i
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Form State'leri (Yeni Ürün Ekleme için)
  const [showAddModal, setShowAddModal] = useState(false);
  const [newProduct, setNewProduct] = useState({
    name: "",
    description: "",
    price: 0,
    stock: 0,
  });

  const navigate = useNavigate();
  const token = getCurrentToken();
  const user = getCurrentUser();

  // 1. Ürünleri Çek
  const fetchProducts = () => {
    api
      .get("/Products")
      .then((response) => {
        setProducts(response.data);
        setLoading(false);
      })
      .catch((err) => {
        console.error("API Hatası:", err);
        setError("Ürünler yüklenirken bir hata oluştu.");
        setLoading(false);
      });
  };

  // 2. Kullanıcının Mevcut Sepetini Çek
  const fetchBasket = () => {
    if (!token) return;
    api
      .get("/basket")
      .then((res) => {
        if (res.data) setBasket(res.data);
      })
      .catch((err) => console.error("Sepet çekilemedi:", err));
  };

  useEffect(() => {
    fetchProducts();
    fetchBasket();
  }, []);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  // 🛒 SEPETE ÜRÜN EKLEME İŞLEMİ
  const handleAddToCart = (product) => {
    if (!token) {
      alert("Sepete ürün eklemek için lütfen giriş yapın!");
      navigate("/login");
      return;
    }

    const currentItems = basket.items ? [...basket.items] : [];
    const existingItemIndex = currentItems.findIndex(
      (item) => item.productId === product.id,
    );

    if (existingItemIndex > -1) {
      // Ürün zaten sepette varsa adedi artır
      currentItems[existingItemIndex].quantity += 1;
    } else {
      // Ürün ilk defa ekleniyorsa listeye push et
      currentItems.push({
        productId: product.id,
        productName: product.name,
        price: product.price,
        quantity: 1,
        imageUrl: "",
      });
    }

    const updatedBasket = { ...basket, items: currentItems };

    // Redis Basket API'ye Kaydet
    api
      .post("/basket", updatedBasket)
      .then((res) => {
        setBasket(res.data);
        alert(`${product.name} sepete eklendi!`);
      })
      .catch((err) => {
        console.error("Sepet güncellenemedi:", err);
        alert("Ürün sepete eklenirken bir hata oluştu.");
      });
  };

  // 🔴 Ürün Silme İşlemi
  const handleDeleteProduct = (id) => {
    if (!window.confirm("Bu ürünü silmek istediğinize emin misiniz?")) return;

    api
      .delete(`/Products/${id}`)
      .then(() => {
        setProducts(products.filter((p) => p.id !== id));
      })
      .catch((err) => {
        console.error("Silme hatası:", err);
        alert("Ürün silinirken bir hata oluştu.");
      });
  };

  // 🟢 Ürün Ekleme İşlemi
  const handleCreateProduct = (e) => {
    e.preventDefault();
    api
      .post("/Products", newProduct)
      .then((res) => {
        setProducts([...products, res.data]);
        setShowAddModal(false);
        setNewProduct({ name: "", description: "", price: 0, stock: 0 });
      })
      .catch((err) => {
        console.error("Ekleme hatası:", err);
        alert("Ürün eklenirken bir hata oluştu.");
      });
  };

  // Sepetteki toplam ürün adedini hesapla
  const totalCartCount =
    basket?.items?.reduce((acc, item) => acc + item.quantity, 0) || 0;

  if (loading)
    return <div style={{ padding: "20px" }}>📦 Ürünler yükleniyor...</div>;
  if (error)
    return <div style={{ padding: "20px", color: "red" }}>❌ {error}</div>;

  return (
    <div style={{ padding: "30px", fontFamily: "Arial, sans-serif" }}>
      {/* Navbar */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          backgroundColor: "#2c3e50",
          padding: "15px 20px",
          color: "#fff",
          borderRadius: "8px",
        }}
      >
        <h2 style={{ margin: 0 }}>🛒 EticaretMicroservice</h2>
        <div style={{ display: "flex", alignItems: "center", gap: "15px" }}>
          {/* 🛒 Sepet Göstergesi (Yeni eklendi) */}
          {token && (
            <Link
              to="/basket"
              style={{
                color: "#fff",
                textDecoration: "none",
                backgroundColor: "#e67e22",
                padding: "8px 12px",
                borderRadius: "20px",
                fontWeight: "bold",
                fontSize: "0.9em",
              }}
            >
              🛒 Sepetim ({totalCartCount})
            </Link>
          )}

          {user && (
            <span
              style={{
                fontSize: "0.9em",
                backgroundColor: "#34495e",
                padding: "5px 10px",
                borderRadius: "4px",
              }}
            >
              👤 {user.email} | Rol:{" "}
              <b
                style={{
                  color: user.role === "Admin" ? "#e74c3c" : "#2ecc71",
                }}
              >
                {user.role}
              </b>
            </span>
          )}

          {token ? (
            <button
              onClick={handleLogout}
              style={{
                padding: "8px 15px",
                backgroundColor: "#e74c3c",
                color: "#fff",
                border: "none",
                borderRadius: "4px",
                cursor: "pointer",
              }}
            >
              Çıkış Yap
            </button>
          ) : (
            <div>
              <Link
                to="/login"
                style={{
                  color: "#fff",
                  marginRight: "15px",
                  textDecoration: "none",
                }}
              >
                Giriş Yap
              </Link>
              <Link
                to="/register"
                style={{ color: "#2ecc71", textDecoration: "none" }}
              >
                Kayıt Ol
              </Link>
            </div>
          )}
        </div>
      </div>

      {/* Başlık ve Admin Ürün Ekle Butonu */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginTop: "20px",
        }}
      >
        <h3>Ürün Kataloğu</h3>

        {user?.role === "Admin" && (
          <button
            onClick={() => setShowAddModal(!showAddModal)}
            style={{
              padding: "10px 15px",
              backgroundColor: "#27ae60",
              color: "#fff",
              border: "none",
              borderRadius: "6px",
              cursor: "pointer",
              fontWeight: "bold",
            }}
          >
            {showAddModal ? "Kapat" : "+ Yeni Ürün Ekle"}
          </button>
        )}
      </div>

      {/* Admin Ürün Ekleme Formu */}
      {showAddModal && user?.role === "Admin" && (
        <form
          onSubmit={handleCreateProduct}
          style={{
            backgroundColor: "#f8f9fa",
            padding: "20px",
            borderRadius: "8px",
            marginTop: "15px",
            border: "1px solid #ddd",
          }}
        >
          <h4>Yeni Ürün Bilgileri</h4>
          <div
            style={{
              display: "grid",
              gap: "10px",
              gridTemplateColumns: "1fr 1fr",
            }}
          >
            <input
              type="text"
              placeholder="Ürün Adı"
              value={newProduct.name}
              onChange={(e) =>
                setNewProduct({ ...newProduct, name: e.target.value })
              }
              required
              style={{ padding: "8px" }}
            />
            <input
              type="text"
              placeholder="Açıklama"
              value={newProduct.description}
              onChange={(e) =>
                setNewProduct({ ...newProduct, description: e.target.value })
              }
              required
              style={{ padding: "8px" }}
            />
            <input
              type="number"
              placeholder="Fiyat"
              value={newProduct.price}
              onChange={(e) =>
                setNewProduct({ ...newProduct, price: Number(e.target.value) })
              }
              required
              style={{ padding: "8px" }}
            />
            <input
              type="number"
              placeholder="Stok"
              value={newProduct.stock}
              onChange={(e) =>
                setNewProduct({ ...newProduct, stock: Number(e.target.value) })
              }
              required
              style={{ padding: "8px" }}
            />
          </div>
          <button
            type="submit"
            style={{
              marginTop: "15px",
              padding: "8px 20px",
              backgroundColor: "#2980b9",
              color: "#fff",
              border: "none",
              borderRadius: "4px",
              cursor: "pointer",
            }}
          >
            Kaydet
          </button>
        </form>
      )}

      {/* Ürün Listesi */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))",
          gap: "20px",
          marginTop: "20px",
        }}
      >
        {products.map((product) => (
          <div
            key={product.id}
            style={{
              border: "1px solid #e0e0e0",
              borderRadius: "12px",
              padding: "20px",
              backgroundColor: "#fff",
              boxShadow: "0 4px 6px rgba(0,0,0,0.05)",
              display: "flex",
              flexDirection: "column",
              justifyContent: "space-between",
            }}
          >
            <div>
              <h3 style={{ margin: "0 0 10px 0", color: "#2980b9" }}>
                {product.name}
              </h3>
              <p style={{ color: "#555", fontSize: "0.95em" }}>
                {product.description}
              </p>
            </div>

            <div>
              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  marginTop: "15px",
                }}
              >
                <span
                  style={{
                    fontWeight: "bold",
                    fontSize: "1.2em",
                    color: "#27ae60",
                  }}
                >
                  {product.price} TL
                </span>
                <span style={{ fontSize: "0.85em", color: "#e67e22" }}>
                  Stok: {product.stock}
                </span>
              </div>

              {/* 🛒 SEPETE EKLE BUTONU */}
              <button
                onClick={() => handleAddToCart(product)}
                style={{
                  width: "100%",
                  marginTop: "15px",
                  padding: "10px",
                  backgroundColor: "#2980b9",
                  color: "#fff",
                  border: "none",
                  borderRadius: "6px",
                  cursor: "pointer",
                  fontWeight: "bold",
                }}
              >
                🛒 Sepete Ekle
              </button>

              {/* 🔴 ADMIN İSE SİL BUTONU */}
              {user?.role === "Admin" && (
                <button
                  onClick={() => handleDeleteProduct(product.id)}
                  style={{
                    width: "100%",
                    marginTop: "8px",
                    padding: "6px",
                    backgroundColor: "#e74c3c",
                    color: "#fff",
                    border: "none",
                    borderRadius: "6px",
                    cursor: "pointer",
                    fontSize: "0.85em",
                  }}
                >
                  🗑️ Ürünü Sil
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
