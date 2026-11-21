import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ChevronRight, Repeat, SearchX, UserPlus, Users, Wallet } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import Badge from "../../../shared/components/Badge";
import Button from "../../../shared/components/Button";
import Dialog from "../../../shared/components/Dialog";
import EmptyState from "../../../shared/components/EmptyState";
import FilterTabs from "../../../shared/components/FilterTabs";
import SearchInput from "../../../shared/components/SearchInput";
import Skeleton from "../../../shared/components/Skeleton";
import { formatDate, formatDay, formatNumber, formatPrice } from "../../../shared/utils/format";
import { normalizeText } from "../../catalog/catalogView";
import { getAllOrders } from "../../orders/api";
import OrderStatusBadge from "../../orders/components/OrderStatusBadge";
import type { Order } from "../../orders/types";
import { getUsers } from "../api";
import { customerStats, type CustomerStats } from "../analytics";
import StatCard from "../components/StatCard";
import ui from "../admin.module.css";
import styles from "./AdminCustomersPage.module.css";

type Segment = "all" | "buyers" | "repeat" | "new";

const SEGMENTS: { value: Segment; label: string }[] = [
  { value: "all", label: "Tümü" },
  { value: "buyers", label: "Sipariş verenler" },
  { value: "repeat", label: "Tekrar alanlar" },
  { value: "new", label: "Henüz sipariş vermeyenler" },
];

const inSegment = (customer: CustomerStats, segment: Segment) => {
  if (segment === "buyers") return customer.completedCount > 0;
  if (segment === "repeat") return customer.completedCount > 1;
  if (segment === "new") return customer.orderCount === 0;
  return true;
};

async function loadCustomers() {
  const [users, orders] = await Promise.all([getUsers(), getAllOrders()]);
  return { customers: customerStats(users, orders), orders };
}

export default function AdminCustomersPage() {
  const { data, error, isLoading, reload } = useAsync(loadCustomers, []);
  const [searchParams, setSearchParams] = useSearchParams();
  const [selected, setSelected] = useState<CustomerStats | null>(null);

  const query = searchParams.get("q") ?? "";
  const segment = SEGMENTS.find((s) => s.value === searchParams.get("segment"))?.value ?? "all";

  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(searchParams);
    if (value) next.set(key, value);
    else next.delete(key);
    setSearchParams(next, { replace: true });
  };

  const customers = data?.customers ?? [];
  const terms = normalizeText(query).split(/\s+/).filter(Boolean);
  const searched = customers.filter((c) => {
    const haystack = normalizeText(`${c.user.username} ${c.user.email} ${c.city ?? ""}`);
    return terms.every((term) => haystack.includes(term));
  });
  const visible = searched
    .filter((c) => inSegment(c, segment))
    .sort((a, b) => b.totalSpent - a.totalSpent || a.user.username.localeCompare(b.user.username, "tr"));

  const buyers = customers.filter((c) => c.completedCount > 0);
  const repeatBuyers = customers.filter((c) => c.completedCount > 1);
  const totalSpent = buyers.reduce((sum, c) => sum + c.totalSpent, 0);

  const selectedOrders: Order[] = selected ? (data?.orders ?? []).filter((o) => o.buyerId === selected.user.id) : [];

  return (
    <>
      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>Müşteriler</h1>
          <p className={ui.subtitle}>Kayıtlı müşteriler ve alışveriş geçmişleri.</p>
        </div>
      </div>

      {isLoading ? (
        <div aria-busy="true" aria-label="Müşteriler yükleniyor">
          <div className={ui.stats}>
            {[1, 2, 3, 4].map((i) => (
              <Skeleton key={i} className={ui.skeletonStat} />
            ))}
          </div>
          {Array.from({ length: 6 }, (_, i) => (
            <Skeleton key={i} className={ui.skeletonRow} />
          ))}
        </div>
      ) : error ? (
        <EmptyState
          icon={Users}
          title="Müşteriler yüklenemedi"
          text={getErrorMessage(error, "Identity veya Order servisine ulaşılamıyor.")}
          action={<Button onClick={reload}>Tekrar dene</Button>}
        />
      ) : (
        <>
          <div className={ui.stats}>
            <StatCard icon={Users} label="Kayıtlı müşteri" value={formatNumber(customers.length)} />
            <StatCard
              icon={UserPlus}
              label="Sipariş veren"
              value={formatNumber(buyers.length)}
              hint={customers.length > 0 ? `Müşterilerin %${Math.round((buyers.length / customers.length) * 100)}'i` : undefined}
            />
            <StatCard
              icon={Repeat}
              label="Tekrar alan"
              value={formatNumber(repeatBuyers.length)}
              hint={buyers.length > 0 ? `Alıcıların %${Math.round((repeatBuyers.length / buyers.length) * 100)}'i` : undefined}
            />
            <StatCard
              icon={Wallet}
              label="Müşteri başına harcama"
              value={formatPrice(buyers.length > 0 ? totalSpent / buyers.length : 0)}
              hint="Sipariş veren müşteriler"
            />
          </div>

          <div className={ui.toolbar}>
            <SearchInput
              className={ui.toolbarSearch}
              value={query}
              onChange={(value) => setParam("q", value || null)}
              label="Müşterilerde ara"
              placeholder="Ad, e-posta veya il"
            />
            <FilterTabs
              label="Segment"
              tabs={SEGMENTS.map((s) => ({ ...s, count: searched.filter((c) => inSegment(c, s.value)).length }))}
              value={segment}
              onChange={(value) => setParam("segment", value === "all" ? null : value)}
            />
          </div>

          {visible.length === 0 ? (
            <EmptyState icon={SearchX} title="Eşleşen müşteri yok" text="Arama terimini veya segmenti değiştirmeyi deneyin." />
          ) : (
            <div className={ui.tableWrap}>
              <table className={ui.table}>
                <thead>
                  <tr>
                    <th>Müşteri</th>
                    <th>İl</th>
                    <th className={ui.alignRight}>Sipariş</th>
                    <th className={ui.alignRight}>Toplam harcama</th>
                    <th>Son sipariş</th>
                    <th>
                      <span className="visually-hidden">İşlem</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {visible.map((customer) => (
                    <tr key={customer.user.id}>
                      <td>
                        <div className={styles.person}>
                          <span className={styles.avatar} aria-hidden="true">
                            {customer.user.username.charAt(0)}
                          </span>
                          <div>
                            <div className={ui.cellTitle}>
                              {customer.user.username} {customer.user.isSample && <Badge>Örnek</Badge>}
                            </div>
                            <div className={ui.cellSub}>{customer.user.email}</div>
                          </div>
                        </div>
                      </td>
                      <td className={ui.muted}>{customer.city ?? "—"}</td>
                      <td className={`${ui.alignRight} ${ui.num}`}>{customer.orderCount}</td>
                      <td className={`${ui.alignRight} ${ui.num} ${ui.strong}`}>{formatPrice(customer.totalSpent)}</td>
                      <td className={`${ui.muted} ${ui.nowrap}`}>
                        {customer.lastOrderAt ? formatDay(customer.lastOrderAt.toISOString()) : "—"}
                      </td>
                      <td className={ui.alignRight}>
                        <button type="button" className={`${ui.rowLink} ${styles.rowButton}`} onClick={() => setSelected(customer)}>
                          Siparişler <ChevronRight size={16} aria-hidden="true" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}

      <Dialog open={selected !== null} onClose={() => setSelected(null)} title={selected?.user.username ?? "Müşteri"} size="lg">
        {selected && (
          <>
            <dl className={styles.summary}>
              <div>
                <dt>E-posta</dt>
                <dd>{selected.user.email}</dd>
              </div>
              <div>
                <dt>Sipariş</dt>
                <dd>{selected.orderCount}</dd>
              </div>
              <div>
                <dt>Toplam harcama</dt>
                <dd>{formatPrice(selected.totalSpent)}</dd>
              </div>
            </dl>
            {selectedOrders.length === 0 ? (
              <EmptyState variant="plain" title="Henüz sipariş yok" text="Bu müşteri kayıt oldu ancak sipariş vermedi." />
            ) : (
              <ul className={styles.orders}>
                {selectedOrders.map((order) => (
                  <li key={order.id}>
                    <Link to={`/admin/orders/${order.id}`} className={styles.order}>
                      <span>
                        <span className={ui.cellTitle}>#{order.id}</span>
                        <span className={ui.cellSub}>{formatDate(order.createdDate)}</span>
                      </span>
                      <OrderStatusBadge status={order.orderStatus} />
                      <span className={`${ui.num} ${ui.strong}`}>{formatPrice(order.totalPrice)}</span>
                      <ChevronRight size={16} aria-hidden="true" />
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </Dialog>
    </>
  );
}
