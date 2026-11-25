import { useAsync } from "../../../shared/hooks/useAsync";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Package } from "lucide-react";
import { getErrorMessage, getErrorStatus } from "../../../lib/apiClient";
import { getMyOrder } from "../api";
import { useOrderHub } from "../useOrderHub";
import OrderStatusBadge from "../components/OrderStatusBadge";
import OrderTimeline from "../components/OrderTimeline";
import Alert from "../../../shared/components/Alert";
import EmptyState from "../../../shared/components/EmptyState";
import { ButtonLink } from "../../../shared/components/Button";
import { formatDate, formatPrice } from "../../../shared/utils/format";
import { PAYMENT_STATUS_LABEL } from "../orderStatus";
import styles from "./MyOrderDetailPage.module.css";

export default function MyOrderDetailPage() {
  const { id } = useParams();
  // Hızla başka siparişe geçilirse eski cevabın yeni sayfanın üzerine yazmamasını useAsync sağlar
  const { data: order, error, isLoading } = useAsync(() => getMyOrder(Number(id)), [id]);
  const { orderStatus: liveStatus } = useOrderHub();

  // Sayfa açıkken sipariş tamamlanır veya iptal edilirse SignalR bildirimi API'den gelen durumun yerine geçer
  const isLiveForThisOrder = liveStatus && liveStatus.orderId === Number(id);
  const status = isLiveForThisOrder ? liveStatus.status : order?.orderStatus;

  if (isLoading) return <p className={styles.state}>Yükleniyor...</p>;

  if (!order) {
    return (
      <EmptyState
        icon={Package}
        title={getErrorStatus(error) === 404 ? "Sipariş bulunamadı." : getErrorMessage(error, "Sipariş şu anda getirilemiyor.")}
        text="Sipariş numarasını kontrol edin veya siparişlerinize dönün."
        action={<ButtonLink to="/orders">Siparişlerime dön</ButtonLink>}
      />
    );
  }

  return (
    <>
      <Link to="/orders" className={styles.back}>
        <ArrowLeft size={16} /> Siparişlerim
      </Link>

      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>Sipariş #{order.id}</h1>
          <p className={styles.date}>{formatDate(order.createdDate)}</p>
        </div>
        <OrderStatusBadge status={status} />
      </div>

      <section className={`${styles.panel} ${styles.progress}`}>
        <h2 className={styles.panelTitle}>Sipariş geçmişi</h2>
        <OrderTimeline entries={order.history} />
        {isLiveForThisOrder && liveStatus.message && <div className={styles.message}>{liveStatus.message}</div>}
      </section>

      <div className={styles.layout}>
        <section className={styles.panel}>
          <h2 className={styles.panelTitle}>Ürünler</h2>
          {order.orderItems?.map((item) => (
            <div key={item.productId} className={styles.item}>
              <div>
                <div className={styles.itemName}>{item.productName}</div>
                <div className={styles.itemQty}>
                  {item.quantity} × {formatPrice(item.price)}
                </div>
              </div>
              <strong>{formatPrice(item.price * item.quantity)}</strong>
            </div>
          ))}
        </section>

        <div className={styles.side}>
          {order.paymentStatus === "IadeEdildi" && (
            <Alert tone="success">
              Ödemeniz ({formatPrice(order.totalPrice)}) {formatDate(order.refundedAt)} tarihinde iade edildi.
            </Alert>
          )}

          <section className={styles.panel}>
            <h2 className={styles.panelTitle}>Özet</h2>
            <div className={styles.summaryRow}>
              <span>Ürün adedi</span>
              <span>{order.orderItems?.reduce((sum, item) => sum + item.quantity, 0) ?? 0}</span>
            </div>
            <div className={styles.summaryRow}>
              <span>Ödeme</span>
              <span>{PAYMENT_STATUS_LABEL[order.paymentStatus]}</span>
            </div>
            <div className={styles.summaryTotal}>
              <span>Toplam</span>
              <span>{formatPrice(order.totalPrice)}</span>
            </div>
          </section>

          {order.address && (
            <section className={styles.panel}>
              <h2 className={styles.panelTitle}>Teslimat adresi</h2>
              <p className={styles.address}>
                {order.address.contactName && (
                  <>
                    <strong>{order.address.contactName}</strong>
                    <br />
                  </>
                )}
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
