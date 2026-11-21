import { Suspense, useState } from "react";
import { Link, NavLink, Outlet, type NavLinkRenderProps } from "react-router-dom";
import {
  Activity,
  BadgePercent,
  Boxes,
  ExternalLink,
  LayoutDashboard,
  LogOut,
  Menu as MenuIcon,
  Package,
  ShoppingCart,
  Tags,
  Users,
} from "lucide-react";
import { useAuth } from "../../features/auth/AuthContext";
import Dialog from "../../shared/components/Dialog";
import IconButton from "../../shared/components/IconButton";
import Logo from "../../shared/components/Logo";
import PageLoader from "../PageLoader";
import styles from "./AdminLayout.module.css";

// Menü, yöneticinin günlük akışına göre gruplanır: önce genel bakış, sonra satış, katalog ve sistem
const MENU = [
  {
    title: "Genel",
    items: [{ to: "/admin", label: "Gösterge paneli", icon: LayoutDashboard, end: true }],
  },
  {
    title: "Satış",
    items: [
      { to: "/admin/orders", label: "Siparişler", icon: ShoppingCart },
      { to: "/admin/customers", label: "Müşteriler", icon: Users },
      { to: "/admin/promotions", label: "İndirimler", icon: BadgePercent },
    ],
  },
  {
    title: "Katalog",
    items: [
      { to: "/admin/products", label: "Ürünler", icon: Package },
      { to: "/admin/categories", label: "Kategoriler", icon: Tags },
      { to: "/admin/stock", label: "Stok takibi", icon: Boxes },
    ],
  },
  {
    title: "Sistem",
    items: [{ to: "/admin/system", label: "Sistem durumu", icon: Activity }],
  },
];

export default function AdminLayout() {
  const { user, logout } = useAuth();
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  // NavLink aktif route'u kendisi bilir: /admin/orders/5 açıkken "Siparişler" de aktif görünür.
  // end: "/admin" yalnızca tam eşleşmede aktif olsun, her admin sayfasında değil.
  const linkClass = ({ isActive }: NavLinkRenderProps) => `${styles.navLink} ${isActive ? styles.navLinkActive : ""}`;

  const navigation = (
    <nav className={styles.nav} aria-label="Yönetim menüsü">
      {MENU.map((group) => (
        <div key={group.title} className={styles.group}>
          <span className={styles.groupTitle}>{group.title}</span>
          {group.items.map(({ to, label, icon: Icon, ...rest }) => (
            <NavLink key={to} to={to} end={"end" in rest} className={linkClass} onClick={() => setIsMenuOpen(false)}>
              <Icon size={18} aria-hidden="true" />
              {label}
            </NavLink>
          ))}
        </div>
      ))}
    </nav>
  );

  const userBlock = (
    <div className={styles.footer}>
      <div className={styles.user}>
        <span className={styles.avatar} aria-hidden="true">
          {(user?.username || "A").charAt(0)}
        </span>
        <span className={styles.userText}>
          <span className={styles.userName}>{user?.username}</span>
          <span className={styles.userEmail}>{user?.email}</span>
        </span>
      </div>
      <Link to="/" className={styles.footerLink}>
        <ExternalLink size={16} aria-hidden="true" /> Mağazayı görüntüle
      </Link>
      <button type="button" className={styles.footerLink} onClick={logout}>
        <LogOut size={16} aria-hidden="true" /> Çıkış yap
      </button>
    </div>
  );

  return (
    <div className={styles.shell}>
      <aside className={styles.sidebar}>
        <Link to="/admin" className={styles.brand}>
          <Logo tone="light" />
          <span className={styles.brandTag}>Yönetim</span>
        </Link>
        {navigation}
        {userBlock}
      </aside>

      <header className={styles.topbar}>
        <IconButton icon={MenuIcon} label="Menüyü aç" onClick={() => setIsMenuOpen(true)} />
        <Link to="/admin" className={styles.topbarBrand}>
          <Logo />
        </Link>
      </header>

      <main className={styles.main}>
        {/* Sayfalar ayrı parça olarak yüklenir: yüklenirken kenar çubuğu yerinde kalır, yalnızca içerik bekler */}
        <Suspense fallback={<PageLoader />}>
          <Outlet />
        </Suspense>
      </main>

      <Dialog
        open={isMenuOpen}
        onClose={() => setIsMenuOpen(false)}
        title="Yönetim"
        placement="left"
        size="sm"
        className={styles.drawerDialog}
      >
        <div className={styles.drawer}>
          {navigation}
          {userBlock}
        </div>
      </Dialog>
    </div>
  );
}
