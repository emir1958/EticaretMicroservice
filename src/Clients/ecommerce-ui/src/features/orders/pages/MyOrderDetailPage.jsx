import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Check, Package, X } from "lucide-react";
import api from "../../services/api";
import { useOrderHub } from "../../hooks/useOrderHub";
import OrderStatusBadge from "../../components/OrderStatusBadge";
import EmptyState from "../../components/ui/EmptyState";
import Button from "../../components/ui/Button";
import { formatDate, formatPrice, getOrderStatusView } from "../../utils/orderStatus";
import styles from "./MyOrderDetail.module.css";

// Siparişin hangi aşamada olduğu: alındı → ödeme → tamamlandı (iptalde ödeme adımı kırmızı)
function OrderSteps({ status }) {
  const tone = getOrderStatusView(status).tone;
  const steps = [
    { label: "Sipariş alındı", state: "done" },
    {
      label: tone === "canceled" ? "İptal edildi" : "Ödeme",
      state: tone === "completed" ? "done" : tone === "canceled" ? "failed" : "current",
    },
    { label: "Tamamlandı", state: tone === "completed" ? "done" : "todo" },
  ];

  return (
    <div className={styles.steps}>
      {steps.map((step) => (
        <div key={step.label} className={`${styles.step} ${styles[step.state] ?? ""}`}>
          <span className={styles.dot}>
            {step.state === "done" && <Check size={16} />}
            {step.state === "failed" && <X size={16} />}
          </span>
          {step.label}
        </div>
      ))}
    </div>
  );
}

export default function MyOrderDetail() {
  const { id } = useParams();
  // Sonuç hangi id için yüklendiğiyle birlikte tutulur; "yükleniyor" ayrı state değil, buradan türetilir
  const [result, setResult] = useState({ id: null, order: null, error: null });
  const { orderStatus: liveStatus } = useOrderHub();

  useEffect(() => {
    // Kullanıcı hızla başka siparişe geçerse eski isteğin geç gelen cevabı yeni sayfanın üzerine yazmasın
    let ignore = false;
    api
      .get(`/api/orders/user/${id}`)
      .then((res) => {
        if (!ignore) setResult({ id, order: res.data, error: null });
      })
      .catch((err) => {
        if (ignore) return;
        const error =
          err.response?.status === 404
            ? "Sipariş bulunamadı."
            : err.response?.data?.message || "Sipariş şu anda getirilemiyor.";
        setResult({ id, order: null, error });
      });
    return () => {
      ignore = true;
    };
  }, [id]);

  const loading = result.id !== id;
  const { order, error } = result;

  // Sayfa açıkken sipariş tamamlanır veya iptal edilirse SignalR bildirimi API'den gelen durumun yerine geçer
  const isLiveForThisOrder = liveStatus && Number(liveStatus.orderId) === Number(id);
  const status = isLiveForThisOrder ? liveStatus.status : order?.orderStatus;

  if (loading) return <p className={styles.state}>Yükleniyor...</p>;

  if (error) {
    return (
      <EmptyState
        icon={Package}
        title={error}
        text="Sipariş numarasını kontrol edin veya siparişlerinize dönün."
        action={<Button to="/orders">Siparişlerime dön</Button>}
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
        <OrderSteps status={status} />
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
          <section className={styles.panel}>
            <h2 className={styles.panelTitle}>Özet</h2>
            <div className={styles.summaryRow}>
              <span>Ürün adedi</span>
              <span>{order.orderItems?.reduce((sum, item) => sum + item.quantity, 0) ?? 0}</span>
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
