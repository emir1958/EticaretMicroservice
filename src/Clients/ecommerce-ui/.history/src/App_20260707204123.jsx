import { useEffect, useState } from "react";
import axios from "axios";

function App() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    // ⚠️ ÇOK KRİTİK: Visual Studio 2022'de Catalog.Api projesini çalıştırdığında
    // Swagger tarayıcında hangi portla açıldıysa (örn: 5001, 7001, 5123 vb.)
    // aşağıdaki port numarasını onunla MUTLAKA değiştir.
    const catalogApiUrl = "http://localhost:5001/api/products";

    axios
      .get(catalogApiUrl)
      .then((response) => {
        setProducts(response.data);
        setLoading(false);
      })
      .catch((err) => {
        console.error("API Hatası:", err);
        setError(
          "Ürünler yüklenirken bir hata oluştu. Port numarasını, API'nin açık olduğunu veya Program.cs içindeki CORS ayarını kontrol edin.",
        );
        setLoading(false);
      });
  }, []);

  if (loading)
    return (
      <div style={{ padding: "20px", fontFamily: "Arial" }}>
        📦 Ürünler yükleniyor...
      </div>
    );
  if (error)
    return (
      <div style={{ padding: "20px", color: "red", fontFamily: "Arial" }}>
        ❌ {error}
      </div>
    );

  return (
    <div
      style={{
        padding: "30px",
        fontFamily: "Arial, sans-serif",
        backgroundColor: "#f9f9f9",
        minHeight: "100vh",
      }}
    >
      <h1 style={{ color: "#2c3e50" }}>
        🛒 EticaretMicroservice - Ürün Kataloğu
      </h1>
      <p style={{ color: "#7f8c8d" }}>
        Backend (Catalog.Api) servisinden dinamik olarak çekilen veriler:
      </p>
      <hr />

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
            <p style={{ color: "#555", fontSize: "0.95em", height: "40px" }}>
              {product.description}
            </p>
            <div
              style={{
                display: "flex",
                justifyContent: "between",
                alignItems: "center",
                marginTop: "15px",
                borderTop: "1px solid #f1f1f1",
                paddingTop: "10px",
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
              <span
                style={{
                  fontSize: "0.85em",
                  color: "#e67e22",
                  marginLeft: "auto",
                  backgroundColor: "#fdf2e9",
                  padding: "4px 8px",
                  borderRadius: "4px",
                }}
              >
                Stok: {product.stock} adet
              </span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export default App;
