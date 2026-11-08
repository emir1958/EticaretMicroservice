import { Link } from "react-router-dom";
import { Eye, ShoppingCart } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import Alert from "../../../shared/components/Alert";
import EmptyState from "../../../shared/components/EmptyState";
import { formatDate, formatPrice } from "../../../shared/utils/format";
import { getAllOrders } from "../../orders/api";
import OrderStatusBadge from "../../orders/components/OrderStatusBadge";
import ui from "../admin.module.css";

export default function AdminOrdersPage() {
  const { data: orders = [], error, isLoading } = useAsync(getAllOrders, []);

  return (
    <>
      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>Siparişler</h1>
          <p className={ui.subtitle}>Mağazaya gelen tüm siparişler ve güncel durumları.</p>
        </div>
      </div>

      {isLoading ? (
        <p className={ui.state}>Yükleniyor...</p>
      ) : error ? (
        <Alert>{getErrorMessage(error, "Siparişlere erişim yetkiniz yok veya servis kapalı.")}</Alert>
      ) : orders.length === 0 ? (
        <EmptyState icon={ShoppingCart} title="Henüz sipariş yok" text="Müşteriler sipariş verdiğinde burada görünecek." />
      ) : (
        <div className={ui.tableWrap}>
          <table className={ui.table}>
            <thead>
              <tr>
                <th>Sipariş</th>
                <th>Müşteri</th>
                <th>Tarih</th>
                <th>Tutar</th>
                <th>Durum</th>
                <th className={ui.alignRight}>
                  <span className="visually-hidden">İşlem</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {orders.map((order) => (
                <tr key={order.id}>
                  <td className={ui.strong}>#{order.id}</td>
                  {/* Müşteri adı Identity servisinde; Order yalnızca kimliği bilir */}
                  <td className={ui.mono} title={order.buyerId}>
                    {order.buyerId?.slice(0, 8)}…
                  </td>
                  <td className={`${ui.muted} ${ui.nowrap}`}>{formatDate(order.createdDate)}</td>
                  <td className={`${ui.strong} ${ui.nowrap}`}>{formatPrice(order.totalPrice)}</td>
                  <td>
                    <OrderStatusBadge status={order.orderStatus} />
                  </td>
                  <td className={ui.alignRight}>
                    <Link to={`/admin/orders/${order.id}`} className={ui.rowLink}>
                      <Eye size={16} /> İncele
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
