import { Link, NavLink, Outlet } from "react-router-dom";
import { LogIn, LogOut, Package, ShieldCheck, ShoppingBag, ShoppingCart } from "lucide-react";
import { useAuth } from "../../features/auth/AuthContext";
import { useCart } from "../../features/cart/CartContext";
import CartDrawer from "../../features/cart/components/CartDrawer";
import Button from "../../shared/components/Button";
import styles from "./CustomerLayout.module.css";

// Müşteri sayfalarının ortak iskeleti: sabit header, içerik alanı, sepet çekmecesi, footer.
// Router'da layout route olarak kullanılır; sayfa içeriği <Outlet /> yerine gelir.
export default function CustomerLayout() {
  const { user, isAuthenticated, isAdmin, logout } = useAuth();
  const { cart, setIsCartOpen } = useCart();
  const cartCount = cart.reduce((sum, item) => sum + item.quantity, 0);
  const displayName = user?.username || user?.email || "Hesabım";

  const navLinkClass = ({ isActive }) => `${styles.navLink} ${isActive ? styles.navLinkActive : ""}`;

  return (
    <div className={styles.shell}>
      <header className={styles.header}>
        <div className={styles.headerInner}>
          <Link to="/" className={styles.brand}>
            <span className={styles.brandMark}>
              <ShoppingBag size={20} />
            </span>
            <span className={styles.brandText}>E-Ticaret</span>
          </Link>

          <nav className={styles.nav}>
            {isAdmin && (
              <Button to="/admin/orders" variant="secondary" size="sm">
                <ShieldCheck size={16} />
                <span className={styles.hideOnMobile}>Yönetici Paneli</span>
              </Button>
            )}

            {isAuthenticated && !isAdmin && (
              <NavLink to="/orders" className={navLinkClass}>
                <Package size={18} />
                <span className={styles.hideOnMobile}>Siparişlerim</span>
              </NavLink>
            )}

            {isAuthenticated ? (
              <>
                <div className={styles.user}>
                  <span className={styles.avatar}>{displayName.charAt(0)}</span>
                  <span className={styles.hideOnMobile}>{displayName}</span>
                </div>
                <button type="button" className={styles.iconButton} onClick={logout} title="Çıkış yap" aria-label="Çıkış yap">
                  <LogOut size={18} />
                </button>
              </>
            ) : (
              <Button to="/auth" variant="ghost" size="sm">
                <LogIn size={16} /> Giriş Yap
              </Button>
            )}

            {!isAdmin && (
              <button
                type="button"
                className={`${styles.iconButton} ${styles.cartButton}`}
                onClick={() => setIsCartOpen(true)}
                aria-label={`Sepet, ${cartCount} ürün`}
              >
                <ShoppingCart size={18} />
                {cartCount > 0 && <span className={styles.cartCount}>{cartCount}</span>}
              </button>
            )}
          </nav>
        </div>
      </header>

      <main className={styles.main}>
        <Outlet />
      </main>

      <CartDrawer />

      <footer className={styles.footer}>
        <div className={styles.footerInner}>© {new Date().getFullYear()} E-Ticaret · Mikroservis demo projesi</div>
      </footer>
    </div>
  );
}
