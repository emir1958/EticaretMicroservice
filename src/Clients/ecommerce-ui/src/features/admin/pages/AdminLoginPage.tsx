import { useState } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { ArrowLeft, Lock, Mail, ShieldCheck } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import Alert from "../../../shared/components/Alert";
import Button from "../../../shared/components/Button";
import FormField from "../../../shared/components/FormField";
import { useAuth } from "../../auth/AuthContext";
import styles from "./AdminLoginPage.module.css";

export default function AdminLoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { login, logout, isAdmin } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  // AdminRoute buraya yönlendirirken gidilmek istenen sayfayı state ile taşır
  const from = location.state?.from?.pathname || "/admin/orders";

  // Zaten yönetici olarak giriş yapılmışsa form gösterilmez
  if (isAdmin) return <Navigate to={from} replace />;

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const loggedUser = await login(email, password);
      if (!loggedUser.isAdmin) {
        // Yönetici olmayan hesap burada oturum açık bırakılmaz: kullanıcı reddedildiğini görürken
        // arka planda müşteri olarak giriş yapmış kalmamalı.
        logout();
        setError("Bu hesap yönetici yetkisine sahip değil.");
        setIsSubmitting(false);
        return;
      }
      navigate(from, { replace: true });
    } catch (err) {
      setError(getErrorMessage(err, "Giriş başarısız."));
      setIsSubmitting(false);
    }
  };

  return (
    <div className={styles.wrap}>
      <div className={styles.card}>
        <div className={styles.heading}>
          <span className={styles.mark}>
            <ShieldCheck size={24} />
          </span>
          <h1 className={styles.title}>Yönetim Paneli</h1>
          <p className={styles.subtitle}>Devam etmek için yönetici hesabınla giriş yap.</p>
        </div>

        {error && <Alert className={styles.alert}>{error}</Alert>}

        <form onSubmit={handleSubmit} className={styles.form}>
          <FormField
            label="E-Posta"
            icon={Mail}
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="admin@eticaret.com"
            autoComplete="username"
          />
          <FormField
            label="Şifre"
            icon={Lock}
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            autoComplete="current-password"
          />
          <Button type="submit" size="lg" fullWidth disabled={isSubmitting}>
            {isSubmitting ? "Giriş yapılıyor..." : "Giriş yap"}
          </Button>
        </form>

        <Link to="/" className={styles.back}>
          <ArrowLeft size={16} /> Mağazaya dön
        </Link>
      </div>
    </div>
  );
}
