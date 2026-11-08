import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { ShieldCheck, Mail, Lock } from "lucide-react";

export default function AdminLogin() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const loggedUser = await login(email, password);
      if (!loggedUser.isAdmin) {
        setError("Yetkisiz Giriş: Bu hesap Admin yetkisine sahip değildir.");
        return;
      }
      navigate("/admin/orders");
    } catch (err) {
      setError(
        err.response?.data?.message || err.message || "Giriş başarısız.",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "#0f172a",
        fontFamily: "sans-serif",
      }}
    >
      <form
        onSubmit={handleSubmit}
        style={{
          background: "#1e293b",
          padding: "36px",
          borderRadius: "16px",
          width: "100%",
          maxWidth: "400px",
          color: "#fff",
          border: "1px solid #334155",
        }}
      >
        <div style={{ textAlign: "center", marginBottom: "24px" }}>
          <ShieldCheck
            size={48}
            color="#3b82f6"
            style={{ margin: "0 auto 12px auto" }}
          />
          <h2 style={{ margin: 0, fontSize: "22px" }}>Admin Paneli Girişi</h2>
        </div>

        {error && (
          <div
            style={{
              background: "#ef444420",
              border: "1px solid #ef4444",
              color: "#f87171",
              padding: "10px",
              borderRadius: "8px",
              fontSize: "13px",
              marginBottom: "16px",
            }}
          >
            {error}
          </div>
        )}

        <div style={{ marginBottom: "16px" }}>
          <label
            style={{
              display: "block",
              fontSize: "13px",
              color: "#94a3b8",
              marginBottom: "6px",
            }}
          >
            E-Posta
          </label>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: "8px",
              background: "#0f172a",
              border: "1px solid #475569",
              padding: "10px 12px",
              borderRadius: "8px",
            }}
          >
            <Mail size={16} color="#64748b" />
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="admin@eticaret.com"
              style={{
                background: "transparent",
                border: "none",
                color: "#fff",
                width: "100%",
                outline: "none",
              }}
            />
          </div>
        </div>

        <div style={{ marginBottom: "24px" }}>
          <label
            style={{
              display: "block",
              fontSize: "13px",
              color: "#94a3b8",
              marginBottom: "6px",
            }}
          >
            Şifre
          </label>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: "8px",
              background: "#0f172a",
              border: "1px solid #475569",
              padding: "10px 12px",
              borderRadius: "8px",
            }}
          >
            <Lock size={16} color="#64748b" />
            <input
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              style={{
                background: "transparent",
                border: "none",
                color: "#fff",
                width: "100%",
                outline: "none",
              }}
            />
          </div>
        </div>

        <button
          type="submit"
          disabled={loading}
          style={{
            width: "100%",
            background: "#2563eb",
            color: "#fff",
            border: "none",
            padding: "12px",
            borderRadius: "8px",
            fontWeight: "bold",
            cursor: loading ? "not-allowed" : "pointer",
          }}
        >
          {loading ? "Giriş yapılıyor..." : "Yönetici Girişi Yap"}
        </button>
      </form>
    </div>
  );
}
