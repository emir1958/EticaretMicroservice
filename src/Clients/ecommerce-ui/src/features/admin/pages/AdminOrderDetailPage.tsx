import { Link, useParams } from "react-router-dom";
import { ArrowLeft, CreditCard, History, MapPin, Package, User } from "lucide-react";
import { getErrorMessage, getErrorStatus } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import Alert from "../../../shared/components/Alert";
import Badge from "../../../shared/components/Badge";
import Button, { ButtonLink } from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import Picture from "../../../shared/components/Picture";
import Skeleton from "../../../shared/components/Skeleton";
import { formatDate, formatPrice } from "../../../shared/utils/format";
import { getProducts } from "../../catalog/api";
import { getOrder } from "../../orders/api";
import OrderStatusBadge, { PaymentStatusBadge } from "../../orders/components/OrderStatusBadge";
import OrderTimeline from "../../orders/components/OrderTimeline";
import { getUsers } from "../api";
import { itemCountOf } from "../analytics";
import ui from "../admin.module.css";
import styles from "./AdminOrderDetailPage.module.css";

// Sipariş tek başına yeterli; müşteri ve ürün görseli başka servislerden gelen ek bilgidir
async function loadOrderDetail(id: number) {
  const [order, users, products] = await Promise.all([
    getOrder(id),
    getUsers().catch(() => []),
    getProducts().catch(() => []),
  ]);
  return {
    order,
    customer: users.find((user) => user.id === order.buyerId) ?? null,
    imageById: new Map(products.map((product) => [product.id, product.imageUrl])),
  };
}

export default function AdminOrderDetailPage() {
  const { id } = useParams();
  const { data, error, isLoading, reload } = useAsync(() => loadOrderDetail(Number(id)), [id]);

  if (isLoading) {
    return (
      <div aria-busy="true" aria-label="Sipariş yükleniyor">
        <Skeleton className={styles.skeletonTitle} />
        <Skeleton className={styles.skeletonBlock} />
      </div>
    );
  }

  if (!data) {
    const isNotFound = getErrorStatus(error) === 404;
    return (
      <EmptyState
        icon={Package}
        title={isNotFound ? "Sipariş bulunamadı" : "Sipariş yüklenemedi"}
        text={isNotFound ? `#${id} numaralı bir sipariş yok.` : getErrorMessage(error, "Sipariş servisine ulaşılamıyor.")}
        action={
          <>
            {!isNotFound && <Button onClick={reload}>Tekrar dene</Button>}
            <ButtonLink to="/admin/orders" variant="secondary">
              Siparişlere dön
            </ButtonLink>
          </>
        }
      />
    );
  }

  const { order, customer, imageById } = data;
  const items = order.orderItems ?? [];

  return (
    <>
      <Link to="/admin/orders" className={ui.back}>
        <ArrowLeft size={16} aria-hidden="true" /> Siparişler
      </Link>

      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>Sipariş #{order.id}</h1>
          <p className={ui.subtitle}>{formatDate(order.createdDate)}</p>
        </div>
        <div className={styles.badges}>
          <PaymentStatusBadge status={order.paymentStatus} />
          <OrderStatusBadge status={order.orderStatus} />
        </div>
      </div>

      {order.orderStatus === "Beklemede" && (
        <Alert tone="info" className={ui.alert}>
          Sipariş stok ve ödeme adımlarını bekliyor. Sonuç geldiğinde durum kendiliğinden güncellenir; ödeme süresi dolarsa
          sipariş iptal edilir.
        </Alert>
      )}

      <div className={styles.layout}>
        <div className={styles.main}>
          <section className={ui.panel}>
            <h2 className={`${ui.panelTitle} ${styles.sectionTitle}`}>
              <Package size={18} aria-hidden="true" /> Ürünler
            </h2>
            <ul className={styles.items}>
              {items.map((item) => (
                <li key={item.productId} className={styles.item}>
                  <Picture src={imageById.get(item.productId)} alt="" width={56} className={styles.thumb} />
                  <div className={styles.itemInfo}>
                    <span className={ui.cellTitle}>{item.productName || item.productId}</span>
                    <span className={ui.cellSub}>
                      {item.quantity} × {formatPrice(item.price)}
                    </span>
                  </div>
                  <span className={`${ui.num} ${ui.strong}`}>{formatPrice(item.price * item.quantity)}</span>
                </li>
              ))}
            </ul>
            <dl className={styles.totals}>
              <div>
                <dt>Ara toplam ({itemCountOf(order)} ürün)</dt>
                <dd>{formatPrice(order.totalPrice)}</dd>
              </div>
              <div>
                <dt>Kargo</dt>
                <dd>Ücretsiz</dd>
              </div>
              <div className={styles.grandTotal}>
                <dt>Toplam</dt>
                <dd>{formatPrice(order.totalPrice)}</dd>
              </div>
            </dl>
          </section>

          <section className={ui.panel}>
            <h2 className={`${ui.panelTitle} ${styles.sectionTitle}`}>
              <History size={18} aria-hidden="true" /> Sipariş geçmişi
            </h2>
            <OrderTimeline entries={order.history} />
          </section>
        </div>

        <div className={styles.side}>
          <section className={ui.panel}>
            <h2 className={`${ui.panelTitle} ${styles.sectionTitle}`}>
              <User size={18} aria-hidden="true" /> Müşteri
            </h2>
            {customer ? (
              <>
                <p className={ui.cellTitle}>
                  {customer.username} {customer.isSample && <Badge>Örnek</Badge>}
                </p>
                <p className={ui.muted}>{customer.email}</p>
                <Link to={`/admin/customers?q=${encodeURIComponent(customer.email)}`} className={styles.inlineLink}>
                  Müşterinin tüm siparişleri
                </Link>
              </>
            ) : (
              <>
                <p className={ui.muted}>Müşteri bilgisi alınamadı.</p>
                <p className={`${ui.mono} ${styles.breakAll}`}>{order.buyerId}</p>
              </>
            )}
          </section>

          <section className={ui.panel}>
            <h2 className={`${ui.panelTitle} ${styles.sectionTitle}`}>
              <CreditCard size={18} aria-hidden="true" /> Ödeme
            </h2>
            <PaymentStatusBadge status={order.paymentStatus} />
            {order.paymentStatus === "IadeEdildi" && (
              <p className={`${ui.muted} ${styles.spaced}`}>İade tarihi: {formatDate(order.refundedAt)}</p>
            )}
          </section>

          {order.address && (
            <section className={ui.panel}>
              <h2 className={`${ui.panelTitle} ${styles.sectionTitle}`}>
                <MapPin size={18} aria-hidden="true" /> Teslimat adresi
              </h2>
              <address className={styles.address}>
                {order.address.contactName && <strong>{order.address.contactName}</strong>}
                <span>
                  {order.address.street} {order.address.line}
                </span>
                <span>
                  {order.address.district} / {order.address.city} {order.address.zipCode}
                </span>
                {order.address.phone && <span>Tel: {order.address.phone}</span>}
              </address>
            </section>
          )}
        </div>
      </div>
    </>
  );
}
