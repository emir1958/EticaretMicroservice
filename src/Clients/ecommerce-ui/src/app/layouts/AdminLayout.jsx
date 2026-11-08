import { Link, NavLink, Outlet } from "react-router-dom";
import { ArrowLeft, LayoutDashboard, LogOut, Package, ShoppingCart } from "lucide-react";
import { useAuth } from "../../features/auth/AuthContext";
import styles from "./AdminLayout.module.css";

const MENU = [
  { to: "/admin/orders", label: "Siparişler", icon: ShoppingCart },
  { to: "/admin/products", label: "Ürün Yönetimi", icon: Package },
];

// Yönetim paneli sayfalarının ortak iskeleti: kenar çubuğu + içerik alanı.
// Router'da layout route olarak kullanılır (bkz. AppRoutes): sayfa içeriği <Outlet /> yerine gelir,
// bu yüzden sayfalar kendilerini <AdminLayout> ile sarmaz.
export default function AdminLayout() {
  const { user, logout } = useAuth();

  // NavLink aktif route'u kendisi bilir: /admin/orders/5 açıkken "Siparişler" de aktif görünür
  const linkClass = ({ isActive }) => `${styles.navLink} ${isActive ? styles.navLinkActive : ""}`;

  return (
    <div className={styles.shell}>
      <aside className={styles.sidebar}>
        <div className={styles.brand}>
          <LayoutDashboard size={20} />
          Yönetim Paneli
        </div>

        <nav className={styles.nav} aria-label="Yönetim menüsü">
          {MENU.map(({ to, label, icon: Icon }) => (
            <NavLink key={to} to={to} className={linkClass}>
              <Icon size={18} />
              {label}
            </NavLink>
          ))}
        </nav>

        <div className={styles.footer}>
          {user?.email && <div className={styles.user}>{user.email}</div>}
          <Link to="/" className={styles.footerLink}>
            <ArrowLeft size={16} /> Mağazaya dön
          </Link>
          <button type="button" className={styles.footerLink} onClick={logout}>
            <LogOut size={16} /> Çıkış yap
          </button>
        </div>
      </aside>

      <main className={styles.main}>
        <Outlet />
      </main>
    </div>
  );
}
