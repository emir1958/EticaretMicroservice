import { useState } from "react";
import { Link } from "react-router-dom";
import { ArrowRight, Banknote, Boxes, Percent, ShoppingCart, Trophy, XCircle } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import Alert from "../../../shared/components/Alert";
import Badge from "../../../shared/components/Badge";
import Button from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import FilterTabs from "../../../shared/components/FilterTabs";
import Picture from "../../../shared/components/Picture";
import Skeleton from "../../../shared/components/Skeleton";
import { formatCompactPrice, formatDate, formatNumber, formatPrice } from "../../../shared/utils/format";
import { getProductsWithStock } from "../../catalog/api";
import { stockStateOf } from "../../catalog/catalogView";
import { getDailySales, getProductSales, getSalesSummary, searchOrders } from "../../orders/api";
import OrderStatusBadge from "../../orders/components/OrderStatusBadge";
import { getUsers } from "../api";
import { percentChange, periodsFor, toDailyPoints, usersById } from "../analytics";
import RevenueChart from "../components/RevenueChart";
import StatCard from "../components/StatCard";
import ui from "../admin.module.css";
import styles from "./AdminDashboardPage.module.css";

type RangeKey = "7" | "30" | "90";

const RANGES: { value: RangeKey; label: string }[] = [
  { value: "7", label: "7 gün" },
  { value: "30", label: "30 gün" },
  { value: "90", label: "90 gün" },
];

// Rakamlar Order servisinde SQL ile hesaplanır; müşteri adları ve ürün görselleri ek bilgi: o servisler yanıt vermezse panel yine açılır
async function loadDashboard(days: number) {
  const { current, previous } = periodsFor(days);
  const [now, before, daily, topProducts, recent, users, products] = await Promise.all([
    getSalesSummary(current),
    getSalesSummary(previous),
    getDailySales(current),
    getProductSales(current, 5),
    searchOrders({ pageSize: 6 }),
    getUsers().catch(() => []),
    getProductsWithStock().catch(() => []),
  ]);
  return { now, before, daily, topProducts, recentOrders: recent.items, users, products };
}

const percent = (value: number) => value.toLocaleString("tr-TR", { style: "percent", maximumFractionDigits: 1 });

export default function AdminDashboardPage() {
  const [range, setRange] = useState<RangeKey>("30");
  const { data, error, isLoading, reload } = useAsync(() => loadDashboard(Number(range)), [range]);

  if (isLoading) return <DashboardSkeleton />;

  if (!data) {
    return (
      <EmptyState
        icon={XCircle}
        title="Gösterge paneli yüklenemedi"
        text={getErrorMessage(error, "Sipariş servisine ulaşılamıyor.")}
        action={<Button onClick={reload}>Tekrar dene</Button>}
      />
    );
  }

  const { now, before, daily, topProducts, recentOrders, users, products } = data;
  const statusTotal = now.completedCount + now.pendingCount + now.canceledCount;
  // offset: yığılmış çubukta parçanın başladığı yer (öncekilerin toplamı)
  const statusParts = [
    { key: "completed", label: "Tamamlanan", count: now.completedCount, offset: 0, className: styles.segCompleted },
    { key: "pending", label: "Bekleyen", count: now.pendingCount, offset: now.completedCount, className: styles.segPending },
    {
      key: "canceled",
      label: "İptal",
      count: now.canceledCount,
      offset: now.completedCount + now.pendingCount,
      className: styles.segCanceled,
    },
  ];

  const topRevenue = topProducts[0]?.revenue ?? 1;
  const imageById = new Map(products.map((p) => [p.id, p.imageUrl]));

  const lowStock = products
    .filter((p) => stockStateOf(p.availableStock) !== "in")
    .sort((a, b) => a.availableStock - b.availableStock)
    .slice(0, 6);

  const userMap = usersById(users);

  return (
    <>
      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>Gösterge paneli</h1>
          <p className={ui.subtitle}>Mağazanın satış, sipariş ve stok durumu.</p>
        </div>
        <FilterTabs label="Dönem" tabs={RANGES} value={range} onChange={setRange} />
      </div>

      <div className={ui.stats}>
        <StatCard icon={Banknote} label="Ciro" value={formatCompactPrice(now.revenue)} change={percentChange(now.revenue, before.revenue)} />
        <StatCard
          icon={ShoppingCart}
          label="Sipariş"
          value={formatNumber(now.orderCount)}
          change={percentChange(now.orderCount, before.orderCount)}
        />
        <StatCard
          icon={Trophy}
          label="Ortalama sepet"
          value={formatPrice(now.averageOrderValue)}
          change={percentChange(now.averageOrderValue, before.averageOrderValue)}
        />
        <StatCard
          icon={Percent}
          label="İptal oranı"
          value={percent(now.cancelRate)}
          change={percentChange(now.cancelRate, before.cancelRate)}
          invert
        />
      </div>

      <div className={styles.grid}>
        <section className={`${ui.panel} ${styles.chartPanel}`}>
          <div className={ui.panelHead}>
            <h2 className={ui.panelTitle}>Günlük ciro</h2>
            <span className={ui.muted}>{formatNumber(now.itemsSold)} ürün satıldı</span>
          </div>
          <RevenueChart points={toDailyPoints(daily)} />
        </section>

        <section className={`${ui.panel} ${styles.statusPanel}`}>
          <div className={ui.panelHead}>
            <h2 className={ui.panelTitle}>Sipariş durumları</h2>
          </div>
          {statusTotal === 0 ? (
            <p className={ui.muted}>Bu dönemde sipariş yok.</p>
          ) : (
            <>
              <svg className={styles.stack} viewBox={`0 0 ${statusTotal} 1`} preserveAspectRatio="none" aria-hidden="true">
                {statusParts.map((part) => (
                  <rect key={part.key} className={part.className} x={part.offset} y={0} width={part.count} height={1} />
                ))}
              </svg>
              <ul className={styles.legend}>
                {statusParts.map((part) => (
                  <li key={part.key}>
                    <span className={`${styles.swatch} ${part.className}`} aria-hidden="true" />
                    <span className={styles.legendLabel}>{part.label}</span>
                    <span className={styles.legendValue}>{part.count}</span>
                    <span className={styles.legendShare}>{percent(part.count / statusTotal)}</span>
                  </li>
                ))}
              </ul>
            </>
          )}
        </section>

        <section className={ui.panel}>
          <div className={ui.panelHead}>
            <h2 className={ui.panelTitle}>En çok satanlar</h2>
          </div>
          {topProducts.length === 0 ? (
            <p className={ui.muted}>Bu dönemde tamamlanan satış yok.</p>
          ) : (
            <ol className={styles.top}>
              {topProducts.map((product) => (
                <li key={product.productId}>
                  <Picture src={imageById.get(product.productId)} alt="" width={48} className={styles.topThumb} />
                  <div className={styles.topBody}>
                    <div className={styles.topRow}>
                      <span className={styles.topName}>{product.name}</span>
                      <span className={ui.num}>{formatPrice(product.revenue)}</span>
                    </div>
                    <svg className={styles.topBar} viewBox="0 0 100 4" preserveAspectRatio="none" aria-hidden="true">
                      <rect className={styles.topTrack} width={100} height={4} rx={2} />
                      <rect className={styles.topFill} width={(product.revenue / topRevenue) * 100} height={4} rx={2} />
                    </svg>
                    <span className={ui.cellSub}>{product.quantity} adet</span>
                  </div>
                </li>
              ))}
            </ol>
          )}
        </section>

        <section className={ui.panel}>
          <div className={ui.panelHead}>
            <h2 className={ui.panelTitle}>
              <Boxes size={18} aria-hidden="true" /> Stok uyarıları
            </h2>
            <Link to="/admin/stock" className={ui.panelLink}>
              Stok takibi <ArrowRight size={14} aria-hidden="true" />
            </Link>
          </div>
          {lowStock.length === 0 ? (
            <p className={ui.muted}>Tüm ürünlerde yeterli stok var.</p>
          ) : (
            <ul className={styles.stockList}>
              {lowStock.map((product) => (
                <li key={product.id}>
                  <span className={styles.stockName}>{product.name}</span>
                  {product.availableStock <= 0 ? (
                    <Badge tone="danger">Tükendi</Badge>
                  ) : (
                    <Badge tone="warning">{product.availableStock} adet</Badge>
                  )}
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className={`${ui.panel} ${styles.recentPanel}`}>
          <div className={ui.panelHead}>
            <h2 className={ui.panelTitle}>Son siparişler</h2>
            <Link to="/admin/orders" className={ui.panelLink}>
              Tüm siparişler <ArrowRight size={14} aria-hidden="true" />
            </Link>
          </div>
          {recentOrders.length === 0 ? (
            <p className={ui.muted}>Henüz sipariş yok.</p>
          ) : (
            <div className={styles.recentTable}>
              <table className={ui.table}>
                <thead>
                  <tr>
                    <th>Sipariş</th>
                    <th>Müşteri</th>
                    <th>Tarih</th>
                    <th>Durum</th>
                    <th className={ui.alignRight}>Tutar</th>
                  </tr>
                </thead>
                <tbody>
                  {recentOrders.map((order) => (
                    <tr key={order.id}>
                      <td>
                        <Link to={`/admin/orders/${order.id}`} className={ui.cellTitle}>
                          #{order.id}
                        </Link>
                      </td>
                      <td>{userMap.get(order.buyerId)?.username ?? order.contactName ?? "—"}</td>
                      <td className={`${ui.muted} ${ui.nowrap}`}>{formatDate(order.createdDate)}</td>
                      <td>
                        <OrderStatusBadge status={order.orderStatus} />
                      </td>
                      <td className={`${ui.alignRight} ${ui.num} ${ui.strong}`}>{formatPrice(order.totalPrice)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>

      {users.length === 0 && (
        <Alert tone="info" className={styles.note}>
          Müşteri listesi alınamadı; siparişlerde müşteri adı yerine teslimat adresindeki isim gösteriliyor.
        </Alert>
      )}
    </>
  );
}

function DashboardSkeleton() {
  return (
    <div aria-busy="true" aria-label="Gösterge paneli yükleniyor">
      <div className={ui.header}>
        <Skeleton className={styles.skeletonTitle} />
      </div>
      <div className={ui.stats}>
        {[1, 2, 3, 4].map((i) => (
          <Skeleton key={i} className={ui.skeletonStat} />
        ))}
      </div>
      <Skeleton className={styles.skeletonChart} />
    </div>
  );
}
