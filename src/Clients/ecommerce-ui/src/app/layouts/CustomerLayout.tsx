import { useEffect, useState, type SubmitEvent } from "react";
import { Link, Outlet, useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { LayoutDashboard, LogOut, Menu as MenuIcon, Package, Search, ShoppingBag, User } from "lucide-react";
import { brand } from "../../config/brand";
import { useAuth } from "../../features/auth/AuthContext";
import { useCart } from "../../features/cart/CartContext";
import CartDrawer from "../../features/cart/components/CartDrawer";
import { getCategories } from "../../features/catalog/api";
import { UNCATEGORIZED } from "../../features/catalog/catalogView";
import { ButtonLink } from "../../shared/components/Button";
import Dialog from "../../shared/components/Dialog";
import IconButton from "../../shared/components/IconButton";
import Logo from "../../shared/components/Logo";
import Menu from "../../shared/components/Menu";
import SearchInput from "../../shared/components/SearchInput";
import { useAsync } from "../../shared/hooks/useAsync";
import styles from "./CustomerLayout.module.css";

// Header'a sığan kategori sayısı; tamamı menü çekmecesinde ve footer'da
const NAV_CATEGORY_LIMIT = 4;
const categoryLink = (name: string) => `/products?category=${encodeURIComponent(name)}`;

export default function CustomerLayout() {
  const { user, isAuthenticated, isAdmin, logout } = useAuth();
  const { cart, setIsCartOpen } = useCart();
  const { pathname } = useLocation();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const { data: categories = [] } = useAsync(getCategories, []);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isSearchOpen, setIsSearchOpen] = useState(false);
  const [query, setQuery] = useState(searchParams.get("q") ?? "");

  // Sayfa değişince yeni sayfa en üstten başlar (tarayıcının kaydırma konumu React'in dışında bir durumdur)
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);

  const cartCount = cart.reduce((sum, item) => sum + item.quantity, 0);
  const displayName = user?.username || user?.email || "Hesabım";
  const navCategories = categories.filter((c) => c.name !== UNCATEGORIZED).slice(0, NAV_CATEGORY_LIMIT);
  const activeCategory = pathname === "/products" ? searchParams.get("category") : null;
  const isSaleActive = pathname === "/products" && searchParams.get("sale") === "1";

  const handleSearch = (e: SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    const trimmed = query.trim();
    navigate(trimmed ? `/products?q=${encodeURIComponent(trimmed)}` : "/products");
    setIsSearchOpen(false);
  };

  const closeMenu = () => setIsMenuOpen(false);

  return (
    <div className={styles.shell}>
      <a href="#icerik" className={styles.skipLink}>
        İçeriğe geç
      </a>

      <div className={styles.announcement}>Tüm siparişlerde ücretsiz kargo · Stok, siparişi verdiğin an senin için ayrılır</div>

      <header className={styles.header}>
        <div className={styles.headerInner}>
          <IconButton icon={MenuIcon} label="Menüyü aç" className={styles.menuButton} onClick={() => setIsMenuOpen(true)} />

          <Link to="/" className={styles.brand} aria-label={`${brand.name} ana sayfa`}>
            <Logo />
          </Link>

          <nav className={styles.nav} aria-label="Kategoriler">
            <Link to="/products" className={styles.navLink} aria-current={pathname === "/products" && !activeCategory && !isSaleActive ? "page" : undefined}>
              Tüm ürünler
            </Link>
            {navCategories.map((category) => (
              <Link
                key={category.name}
                to={categoryLink(category.name)}
                className={styles.navLink}
                aria-current={activeCategory === category.name ? "page" : undefined}
              >
                {category.name}
              </Link>
            ))}
            <Link to="/products?sale=1" className={`${styles.navLink} ${styles.navSale}`} aria-current={isSaleActive ? "page" : undefined}>
              İndirim
            </Link>
          </nav>

          <div className={styles.actions}>
            <form className={styles.search} role="search" onSubmit={handleSearch}>
              <SearchInput value={query} onChange={setQuery} label="Ürün ara" placeholder="Ürün ara" />
            </form>
            <IconButton
              icon={Search}
              label="Ara"
              className={styles.searchToggle}
              aria-expanded={isSearchOpen}
              onClick={() => setIsSearchOpen((open) => !open)}
            />

            {isAuthenticated ? (
              <Menu
                triggerLabel="Hesap menüsü"
                triggerClassName={styles.account}
                trigger={
                  <>
                    <span className={styles.avatar} aria-hidden="true">
                      {displayName.charAt(0)}
                    </span>
                    <span className={styles.accountName}>{displayName}</span>
                  </>
                }
              >
                {isAdmin ? (
                  <Link to="/admin">
                    <LayoutDashboard size={18} aria-hidden="true" /> Yönetim paneli
                  </Link>
                ) : (
                  <>
                    <Link to="/account">
                      <User size={18} aria-hidden="true" /> Hesabım
                    </Link>
                    <Link to="/orders">
                      <Package size={18} aria-hidden="true" /> Siparişlerim
                    </Link>
                  </>
                )}
                <hr />
                <button type="button" onClick={logout}>
                  <LogOut size={18} aria-hidden="true" /> Çıkış yap
                </button>
              </Menu>
            ) : (
              <ButtonLink to="/auth" variant="ghost" size="sm" className={styles.login}>
                <User size={18} aria-hidden="true" />
                <span className={styles.loginText}>Giriş yap</span>
              </ButtonLink>
            )}

            {!isAdmin && (
              <IconButton icon={ShoppingBag} label="Sepet" badge={cartCount} onClick={() => setIsCartOpen(true)} />
            )}
          </div>
        </div>

        {isSearchOpen && (
          <form className={styles.mobileSearch} role="search" onSubmit={handleSearch}>
            <SearchInput value={query} onChange={setQuery} label="Ürün ara" placeholder="Ürün ara" autoFocus />
          </form>
        )}
      </header>

      <main id="icerik" className={styles.main}>
        <Outlet />
      </main>

      <CartDrawer />

      <Dialog open={isMenuOpen} onClose={closeMenu} title="Menü" placement="left" size="sm">
        <nav className={styles.drawerNav} aria-label="Menü" onClick={closeMenu}>
          <Link to="/products">Tüm ürünler</Link>
          {categories
            .filter((c) => c.name !== UNCATEGORIZED)
            .map((category) => (
              <Link key={category.name} to={categoryLink(category.name)}>
                {category.name}
                <span className={styles.drawerCount}>{category.count}</span>
              </Link>
            ))}
          <Link to="/products?sale=1" className={styles.navSale}>
            İndirimdekiler
          </Link>
          <hr />
          {isAdmin ? (
            <Link to="/admin">Yönetim paneli</Link>
          ) : isAuthenticated ? (
            <>
              <Link to="/account">Hesabım</Link>
              <Link to="/orders">Siparişlerim</Link>
            </>
          ) : (
            <Link to="/auth">Giriş yap / Kayıt ol</Link>
          )}
        </nav>
      </Dialog>

      <footer className={styles.footer}>
        <div className={styles.footerInner}>
          <div className={styles.footerBrand}>
            <Logo tone="light" />
            <p>{brand.description}</p>
          </div>

          <nav className={styles.footerColumn} aria-label="Alışveriş">
            <h2>Alışveriş</h2>
            <Link to="/products">Tüm ürünler</Link>
            {navCategories.map((category) => (
              <Link key={category.name} to={categoryLink(category.name)}>
                {category.name}
              </Link>
            ))}
            <Link to="/products?sale=1">İndirimdekiler</Link>
          </nav>

          <nav className={styles.footerColumn} aria-label="Hesap">
            <h2>Hesap</h2>
            {isAuthenticated ? <Link to="/account">Hesabım</Link> : <Link to="/auth">Giriş yap / Kayıt ol</Link>}
            <Link to="/orders">Siparişlerim</Link>
            <Link to="/admin">Yönetim paneli</Link>
          </nav>

          <div className={styles.footerColumn}>
            <h2>Proje hakkında</h2>
            <p>
              {brand.name}, .NET mikroservisleri üzerinde çalışan bir demo mağazadır. Ödemeler simüle edilir, gerçek bir
              satış yapılmaz.
            </p>
          </div>
        </div>
        <div className={styles.footerBottom}>
          © {new Date().getFullYear()} {brand.name} · Mikroservis demo projesi
        </div>
      </footer>
    </div>
  );
}
