import { Link, useParams } from "react-router-dom";
import { ArrowLeft, CreditCard, History, MapPin, Package, User } from "lucide-react";
import { getErrorMessage, getErrorStatus } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import { ButtonLink } from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import { formatDate, formatPrice } from "../../../shared/utils/format";
import { getOrder } from "../../orders/api";
import OrderStatusBadge from "../../orders/components/OrderStatusBadge";
import OrderTimeline from "../../orders/components/OrderTimeline";
import { PAYMENT_STATUS_LABEL } from "../../orders/orderStatus";
import ui from "../admin.module.css";
import styles from "./AdminOrderDetailPage.module.css";

export default function AdminOrderDetailPage() {
  const { id } = useParams();
  const { data: order, error, isLoading } = useAsync(() => getOrder(Number(id)), [id]);

  if (isLoading) return <p className={ui.state}>Yükleniyor...</p>;

  if (!order) {
    return (
      <EmptyState
        icon={Package}
        title={
          getErrorStatus(error) === 404 ? "Sipariş bulunamadı." : getErrorMessage(error, "Sipariş getirilemedi.")
        }
        action={<ButtonLink to="/admin/orders">Siparişlere dön</ButtonLink>}
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

          <section className={ui.panel}>
            <h2 className={ui.panelTitle}>
              <CreditCard size={20} /> Ödeme
            </h2>
            <p className={ui.strong}>{PAYMENT_STATUS_LABEL[order.paymentStatus]}</p>
            {order.paymentStatus === "IadeEdildi" && (
              <p className={ui.muted}>İade tarihi: {formatDate(order.refundedAt)}</p>
            )}
          </section>

          <section className={ui.panel}>
            <h2 className={ui.panelTitle}>
              <History size={20} /> Sipariş geçmişi
            </h2>
            <OrderTimeline entries={order.history} />
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
              {order.address.contactName && <p className={ui.strong}>{order.address.contactName}</p>}
              {order.address.phone && <p className={ui.muted}>Tel: {order.address.phone}</p>}
            </section>
          )}
        </div>
      </div>
    </>
  );
}
