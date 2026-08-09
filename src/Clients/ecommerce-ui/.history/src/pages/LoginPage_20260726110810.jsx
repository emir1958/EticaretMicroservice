import { useState } from "react";
import { login } from "../services/authService";
import { useNavigate, Link } from "react-router-dom";

export default function LoginPage() {
  const [formData, setFormData] = useState({ email: "", password: "" });
  const [error, setError] = useState("");
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    try {
      await login(formData.email, formData.password);
      // Giriş başarılıysa ana sayfaya (ürün kataloğuna) yönlendir
      navigate("/");
    } catch (err) {
      setError(err.response?.data?.message || "E-posta veya şifre hatalı.");
    }
  };

  return (
    <div
      style={{
        maxWidth: "400px",
        margin: "50px auto",
        padding: "20px",
        border: "1px solid #ccc",
        borderRadius: "8px",
      }}
    >
      <h2>🔐 Giriş Yap</h2>
      {error && <p style={{ color: "red" }}>{error}</p>}
      <form onSubmit={handleSubmit}>
        <div style={{ marginBottom: "10px" }}>
          <label>E-posta:</label>
          <input
            type="email"
            required
            style={{ width: "100%", padding: "8px", marginTop: "5px" }}
            onChange={(e) =>
              setFormData({ ...formData, email: e.target.value })
            }
          />
        </div>
        <div style={{ marginBottom: "15px" }}>
          <label>Şifre:</label>
          <input
            type="password"
            required
            style={{ width: "100%", padding: "8px", marginTop: "5px" }}
            onChange={(e) =>
              setFormData({ ...formData, password: e.target.value })
            }
          />
        </div>
        <button
          type="submit"
          style={{
            width: "100%",
            padding: "10px",
            backgroundColor: "#2980b9",
            color: "#fff",
            border: "none",
            borderRadius: "4px",
            cursor: "pointer",
          }}
        >
          Giriş Yap
        </button>
      </form>
      <p style={{ marginTop: "15px", textAlign: "center" }}>
        Hesabınız yok mu? <Link to="/register">Kayıt Ol</Link>
      </p>
    </div>
  );
}
