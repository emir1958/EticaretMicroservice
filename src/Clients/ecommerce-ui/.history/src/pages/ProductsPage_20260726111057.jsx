import { useEffect, useState } from "react";
import axios from "axios";
import { logout, getCurrentToken } from "../services/authService";
import { useNavigate, Link } from "react-router-dom";

export default function ProductsPage() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const navigate = useNavigate();
  const token = getCurrentToken();

  useEffect(() => {
    const catalogApiUrl = "http://localhost:5234/api/Products";

    axios
      .get(catalogApiUrl)
      .then((response) => {
        setProducts(response.data);
        setLoading(false);
      })
      .catch((err) => {
        console.error("API Hatası:", err);
        setError("Ürünler yüklenirken bir hata oluştu.");
        setLoading(false);
      });
  }, []);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  if (loading)
    return <div style={{ padding: "20px" }}>📦 Ürünler yükleniyor...</div>;
  if (error)
    return <div style={{ padding: "20px", color: "red" }}>❌ {error}</div>;

  return (
    <div style={{ padding: "30px", fontFamily: "Arial, sans-serif" }}>
      {/* Navbar Alanı */}
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
        <div>
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

      <h3 style={{ marginTop: "20px" }}>Ürün Kataloğu</h3>
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
            }}
          >
            <h3 style={{ margin: "0 0 10px 0", color: "#2980b9" }}>
              {product.name}
            </h3>
            <p style={{ color: "#555", fontSize: "0.95em" }}>
              {product.description}
            </p>
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
          </div>
        ))}
      </div>
    </div>
  );
}
