import React, { useState } from "react";
import { useAuth } from "../context/AuthContext";
import { useNavigate, useLocation, Link } from "react-router-dom";
import { Lock, Mail, User, ArrowLeft } from "lucide-react";

export default function Auth() {
  const [isLogin, setIsLogin] = useState(true);
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);

  const { login, register } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = location.state?.from?.pathname || "/";

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      if (isLogin) {
        await login(email, password);
        navigate(from, { replace: true });
      } else {
        // Kayıt isteği (RegisterDto: username, email, password)
        await register(username, email, password);
        // Kayıt başarılı olduğunda kullanıcıyı bekletmeden otomatik login yapıyoruz
        await login(email, password);
        navigate(from, { replace: true });
      }
    } catch (err) {
      console.error("Auth Hatası:", err);
      setError(
        err.response?.data?.Message ||
          err.response?.data?.message ||
          err.response?.data?.detail ||
          "Giriş yapılamadı.",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        maxWidth: "420px",
        margin: "60px auto",
        padding: "32px",
        border: "1px solid #e5e7eb",
        borderRadius: "16px",
        boxShadow: "0 4px 6px -1px rgba(0,0,0,0.1)",
        fontFamily: "sans-serif",
        background: "#fff",
      }}
    >
      <Link
        to="/"
        style={{
          display: "inline-flex",
          alignItems: "center",
          gap: "6px",
          textDecoration: "none",
          color: "#64748b",
          marginBottom: "20px",
          fontSize: "14px",
        }}
      >
        <ArrowLeft size={16} /> Mağazaya Dön
      </Link>

      <div
        style={{
          display: "flex",
          borderBottom: "1px solid #e2e8f0",
          marginBottom: "24px",
        }}
      >
        <button
          onClick={() => {
            setIsLogin(true);
            setError(null);
          }}
          style={{
            flex: 1,
            padding: "12px",
            border: "none",
            background: "transparent",
            fontWeight: isLogin ? "bold" : "normal",
            borderBottom: isLogin ? "2px solid #2563eb" : "none",
            color: isLogin ? "#2563eb" : "#64748b",
            cursor: "pointer",
          }}
        >
          Giriş Yap
        </button>
        <button
          onClick={() => {
            setIsLogin(false);
            setError(null);
          }}
          style={{
            flex: 1,
            padding: "12px",
            border: "none",
            background: "transparent",
            fontWeight: !isLogin ? "bold" : "normal",
            borderBottom: !isLogin ? "2px solid #2563eb" : "none",
            color: !isLogin ? "#2563eb" : "#64748b",
            cursor: "pointer",
          }}
        >
          Kayıt Ol
        </button>
      </div>

      {error && (
        <div
          style={{
            background: "#fee2e2",
            color: "#ef4444",
            padding: "10px",
            borderRadius: "8px",
            marginBottom: "16px",
            fontSize: "14px",
          }}
        >
          {error}
        </div>
      )}

      <form
        onSubmit={handleSubmit}
        style={{ display: "flex", flexDirection: "column", gap: "16px" }}
      >
        {!isLogin && (
          <div>
            <label
              style={{
                display: "block",
                fontSize: "13px",
                fontWeight: "500",
                marginBottom: "6px",
                color: "#374151",
              }}
            >
              Kullanıcı Adı
            </label>
            <div style={{ position: "relative" }}>
              <User
                size={18}
                style={{
                  position: "absolute",
                  left: "12px",
                  top: "12px",
                  color: "#9ca3af",
                }}
              />
              <input
                type="text"
                required
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="ornek_kullanici"
                style={{
                  width: "100%",
                  padding: "10px 10px 10px 38px",
                  borderRadius: "8px",
                  border: "1px solid #d1d5db",
                  boxSizing: "border-box",
                }}
              />
            </div>
          </div>
        )}

        <div>
          <label
            style={{
              display: "block",
              fontSize: "13px",
              fontWeight: "500",
              marginBottom: "6px",
              color: "#374151",
            }}
          >
            E-Posta Adresi
          </label>
          <div style={{ position: "relative" }}>
            <Mail
              size={18}
              style={{
                position: "absolute",
                left: "12px",
                top: "12px",
                color: "#9ca3af",
              }}
            />
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="ornek@mail.com"
              style={{
                width: "100%",
                padding: "10px 10px 10px 38px",
                borderRadius: "8px",
                border: "1px solid #d1d5db",
                boxSizing: "border-box",
              }}
            />
          </div>
        </div>

        <div>
          <label
            style={{
              display: "block",
              fontSize: "13px",
              fontWeight: "500",
              marginBottom: "6px",
              color: "#374151",
            }}
          >
            Şifre
          </label>
          <div style={{ position: "relative" }}>
            <Lock
              size={18}
              style={{
                position: "absolute",
                left: "12px",
                top: "12px",
                color: "#9ca3af",
              }}
            />
            <input
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              style={{
                width: "100%",
                padding: "10px 10px 10px 38px",
                borderRadius: "8px",
                border: "1px solid #d1d5db",
                boxSizing: "border-box",
              }}
            />
          </div>
        </div>

        <button
          type="submit"
          disabled={loading}
          style={{
            background: "#2563eb",
            color: "#fff",
            border: "none",
            padding: "12px",
            borderRadius: "8px",
            fontWeight: "bold",
            cursor: loading ? "not-allowed" : "pointer",
            marginTop: "8px",
          }}
        >
          {loading
            ? "İşleniyor..."
            : isLogin
              ? "Giriş Yap"
              : "Kayıt Ol ve Giriş Yap"}
        </button>
      </form>
    </div>
  );
}
