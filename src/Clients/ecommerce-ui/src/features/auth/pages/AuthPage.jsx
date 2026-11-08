import { useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { Lock, Mail, User, ShoppingBag } from "lucide-react";
import { useAuth } from "../AuthContext";
import { getErrorMessage } from "../../../lib/apiClient";
import Alert from "../../../shared/components/Alert";
import Button from "../../../shared/components/Button";
import FormField from "../../../shared/components/FormField";
import styles from "./AuthPage.module.css";

function safeInternalPath(value) {
  if (typeof value !== "string") return null;
  if (
    !value.startsWith("/") ||
    value.startsWith("//") ||
    value.startsWith("/\\")
  )
    return null;
  return value;
}

export default function AuthPage() {
  const [isLogin, setIsLogin] = useState(true);
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);

  const { login, register } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const stateFrom = location.state?.from;
  const redirectParam = new URLSearchParams(location.search).get("redirect");
  const from =
    safeInternalPath(
      stateFrom && `${stateFrom.pathname}${stateFrom.search ?? ""}`,
    ) ||
    safeInternalPath(redirectParam) ||
    "/";

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      if (isLogin) {
        await login(email, password);
        navigate(from, { replace: true });
      } else {
        await register(username, email, password);

        await login(email, password);
        navigate(from, { replace: true });
      }
    } catch (err) {
      console.error("Auth Hatası:", err);
      setError(getErrorMessage(err, "Giriş yapılamadı."));
    } finally {
      setLoading(false);
    }
  };

  const switchTo = (loginMode) => {
    setIsLogin(loginMode);
    setError(null);
  };

  return (
    <div className={styles.wrap}>
      <div className={styles.card}>
        <div className={styles.heading}>
          <span className={styles.mark}>
            <ShoppingBag size={24} />
          </span>
          <h1 className={styles.title}>
            {isLogin ? "Tekrar hoş geldin" : "Hesap oluştur"}
          </h1>
          <p className={styles.subtitle}>
            {isLogin
              ? "Siparişlerine ulaşmak için giriş yap."
              : "Birkaç saniyede kayıt ol ve alışverişe başla."}
          </p>
        </div>

        <div className={styles.tabs} role="tablist">
          <button
            type="button"
            role="tab"
            aria-selected={isLogin}
            className={`${styles.tab} ${isLogin ? styles.tabActive : ""}`}
            onClick={() => switchTo(true)}
          >
            Giriş Yap
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={!isLogin}
            className={`${styles.tab} ${!isLogin ? styles.tabActive : ""}`}
            onClick={() => switchTo(false)}
          >
            Kayıt Ol
          </button>
        </div>

        {error && <Alert className={styles.error}>{error}</Alert>}

        <form onSubmit={handleSubmit} className={styles.form}>
          {!isLogin && (
            <FormField
              required
              label="Kullanıcı Adı"
              icon={User}
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="ornek_kullanici"
              autoComplete="username"
            />
          )}
          <FormField
            required
            label="E-Posta Adresi"
            icon={Mail}
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="ornek@mail.com"
            autoComplete="email"
          />
          <FormField
            required
            label="Şifre"
            icon={Lock}
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            autoComplete={isLogin ? "current-password" : "new-password"}
          />

          <Button
            type="submit"
            size="lg"
            fullWidth
            disabled={loading}
            className={styles.submit}
          >
            {loading
              ? "İşleniyor..."
              : isLogin
                ? "Giriş Yap"
                : "Kayıt Ol ve Giriş Yap"}
          </Button>
        </form>
      </div>
    </div>
  );
}
