import { Link, useParams } from "react-router-dom";
import { ArrowLeft, MapPin, Package, User } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import Button from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import { formatDate, formatPrice } from "../../../shared/utils/format";
import { getOrder } from "../../orders/api";
import OrderStatusBadge from "../../orders/components/OrderStatusBadge";
import ui from "../admin.module.css";
import styles from "./AdminOrderDetailPage.module.css";

export default function AdminOrderDetailPage() {
  const { id } = useParams();
  const { data: order, error, isLoading } = useAsync(() => getOrder(id), [id]);

  if (isLoading) return <p className={ui.state}>Yükleniyor...</p>;

  if (error) {
    return (
      <EmptyState
        icon={Package}
        title={
          error.response?.status === 404 ? "Sipariş bulunamadı." : getErrorMessage(error, "Sipariş getirilemedi.")
        }
        action={<Button to="/admin/orders">Siparişlere dön</Button>}
      />
    );
  }

  const items = order.orderItems ?? [];
  const itemCount = items.reduce((sum, item) => sum + item.quantity, 0);

  return (
    <>
      <Link to="/admin/orders" className={ui.back}>
        <ArrowLeft size={16} /> Siparişler
      </Link>

      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>Sipariş #{order.id}</h1>
          <p className={ui.subtitle}>{formatDate(order.createdDate)}</p>
        </div>
        <OrderStatusBadge status={order.orderStatus} />
      </div>

      <div className={styles.layout}>
        <section className={ui.panel}>
          <h2 className={ui.panelTitle}>
            <Package size={20} /> Ürünler
          </h2>
          {items.map((item) => (
            <div key={item.productId} className={styles.item}>
              <div>
                <div className={styles.itemName}>{item.productName || item.productId}</div>
                <div className={ui.muted}>
                  {item.quantity} × {formatPrice(item.price)}
                </div>
              </div>
              <strong>{formatPrice(item.price * item.quantity)}</strong>
            </div>
          ))}
          <div className={styles.total}>
            <span>Toplam ({itemCount} ürün)</span>
            <span>{formatPrice(order.totalPrice)}</span>
          </div>
        </section>

        <div className={styles.side}>
          <section className={ui.panel}>
            <h2 className={ui.panelTitle}>
              <User size={20} /> Müşteri
            </h2>
            <p className={ui.muted}>Müşteri kimliği</p>
            <p className={`${ui.mono} ${styles.breakAll}`}>{order.buyerId}</p>
          </section>

          {order.address && (
            <section className={ui.panel}>
              <h2 className={ui.panelTitle}>
                <MapPin size={20} /> Teslimat adresi
              </h2>
              <p className={ui.muted}>
                {order.address.street} {order.address.line}
                <br />
                {order.address.district} / {order.address.city} {order.address.zipCode}
              </p>
            </section>
          )}
        </div>
      </div>
    </>
  );
}
