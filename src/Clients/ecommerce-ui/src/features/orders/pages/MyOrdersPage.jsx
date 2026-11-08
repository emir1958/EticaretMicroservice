import { useAsync } from "../../../shared/hooks/useAsync";
import { Link } from "react-router-dom";
import { ChevronRight, Package } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { getMyOrders } from "../api";
import OrderStatusBadge from "../components/OrderStatusBadge";
import Button from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import { formatDate, formatPrice } from "../../../shared/utils/format";
import styles from "./MyOrdersPage.module.css";

export default function MyOrdersPage() {
  const { data: orders = [], error, isLoading } = useAsync(getMyOrders, []);

  return (
    <>
      <div className={styles.header}>
        <h1 className={styles.title}>Siparişlerim</h1>
        <p className={styles.subtitle}>Verdiğin tüm siparişler ve güncel durumları.</p>
      </div>

      {isLoading ? (
        <p className={styles.state}>Yükleniyor...</p>
      ) : error ? (
        <p className={styles.error}>{getErrorMessage(error, "Siparişleriniz şu anda getirilemiyor.")}</p>
      ) : orders.length === 0 ? (
        <EmptyState
          icon={Package}
          title="Henüz bir siparişin yok"
          text="Sipariş verdiğinde durumunu buradan takip edebilirsin."
          action={<Button to="/">Ürünlere göz at</Button>}
        />
      ) : (
        <div className={styles.list}>
          {orders.map((order) => {
            const itemCount = order.orderItems?.reduce((sum, item) => sum + item.quantity, 0) ?? 0;
            return (
              <Link key={order.id} to={`/orders/${order.id}`} className={styles.order}>
                <div className={styles.thumbs}>
                  <span className={styles.thumb}>
                    <Package size={20} />
                  </span>
                </div>
                <div>
                  <div className={styles.orderNo}>Sipariş #{order.id}</div>
                  <div className={styles.meta}>
                    {formatDate(order.createdDate)} · {itemCount} ürün
                  </div>
                </div>
                <OrderStatusBadge status={order.orderStatus} />
                <span className={styles.total}>{formatPrice(order.totalPrice)}</span>
                <ChevronRight size={18} className={styles.chevron} />
              </Link>
            );
          })}
        </div>
      )}
    </>
  );
}
