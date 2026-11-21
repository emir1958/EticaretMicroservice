import { Link, Navigate } from "react-router-dom";
import { ChevronRight, LogOut, Mail, Package, ShoppingBag } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import Alert from "../../../shared/components/Alert";
import Button, { ButtonLink } from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import Skeleton from "../../../shared/components/Skeleton";
import { formatDate, formatPrice } from "../../../shared/utils/format";
import { useAuth } from "../../auth/AuthContext";
import { getMyOrders } from "../../orders/api";
import OrderStatusBadge from "../../orders/components/OrderStatusBadge";
import styles from "./AccountPage.module.css";

export default function AccountPage() {
  const { user, isAdmin, logout } = useAuth();
  const { data: orders = [], error, isLoading } = useAsync(getMyOrders, []);

  // Yöneticinin müşteri hesabı yok; kendi paneline yönlendirilir
  if (isAdmin) return <Navigate to="/admin" replace />;

  const completed = orders.filter((order) => order.orderStatus === "Tamamlandı");
  const pending = orders.filter((order) => order.orderStatus === "Beklemede");
  const totalSpent = completed.reduce((sum, order) => sum + order.totalPrice, 0);

  return (
    <>
      <header className={styles.header}>
        <span className={styles.avatar} aria-hidden="true">
          {(user?.username || "?").charAt(0)}
        </span>
        <div>
          <h1 className={styles.title}>Merhaba, {user?.username}</h1>
          <p className={styles.email}>
            <Mail size={14} aria-hidden="true" /> {user?.email}
          </p>
        </div>
        <Button variant="secondary" onClick={logout} className={styles.logout}>
          <LogOut size={16} aria-hidden="true" /> Çıkış yap
        </Button>
      </header>

      <dl className={styles.stats}>
        <div>
          <dt>Sipariş</dt>
          <dd>{isLoading ? "–" : orders.length}</dd>
        </div>
        <div>
          <dt>Yolda / işleniyor</dt>
          <dd>{isLoading ? "–" : pending.length}</dd>
        </div>
        <div>
          <dt>Toplam alışveriş</dt>
          <dd>{isLoading ? "–" : formatPrice(totalSpent)}</dd>
        </div>
      </dl>

      <section className={styles.section}>
        <div className={styles.sectionHead}>
          <h2 className={styles.sectionTitle}>Son siparişler</h2>
          {orders.length > 0 && (
            <Link to="/orders" className={styles.sectionLink}>
              Tümünü gör <ChevronRight size={16} aria-hidden="true" />
            </Link>
          )}
        </div>

        {isLoading ? (
          <div aria-busy="true">
            {[1, 2, 3].map((i) => (
              <Skeleton key={i} className={styles.skeleton} />
            ))}
          </div>
        ) : error ? (
          <Alert>{getErrorMessage(error, "Siparişlerin şu anda getirilemiyor.")}</Alert>
        ) : orders.length === 0 ? (
          <EmptyState
            icon={ShoppingBag}
            title="Henüz siparişin yok"
            text="İlk siparişini verdiğinde burada takip edebilirsin."
            action={<ButtonLink to="/products">Alışverişe başla</ButtonLink>}
          />
        ) : (
          <ul className={styles.orders}>
            {orders.slice(0, 4).map((order) => (
              <li key={order.id}>
                <Link to={`/orders/${order.id}`} className={styles.order}>
                  <span className={styles.orderIcon} aria-hidden="true">
                    <Package size={18} />
                  </span>
                  <span className={styles.orderInfo}>
                    <span className={styles.orderNo}>Sipariş #{order.id}</span>
                    <span className={styles.orderDate}>{formatDate(order.createdDate)}</span>
                  </span>
                  <OrderStatusBadge status={order.orderStatus} />
                  <span className={styles.orderTotal}>{formatPrice(order.totalPrice)}</span>
                  <ChevronRight size={18} className={styles.chevron} aria-hidden="true" />
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>
    </>
  );
}
