import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ChevronRight, Repeat, SearchX, UserPlus, Users, Wallet } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import { useDebouncedValue } from "../../../shared/hooks/useDebouncedValue";
import { useUrlSearchInput } from "../../../shared/hooks/useUrlSearchInput";
import Badge from "../../../shared/components/Badge";
import Button from "../../../shared/components/Button";
import Dialog from "../../../shared/components/Dialog";
import EmptyState from "../../../shared/components/EmptyState";
import Pagination from "../../../shared/components/Pagination";
import SearchInput from "../../../shared/components/SearchInput";
import Skeleton from "../../../shared/components/Skeleton";
import { formatDate, formatDay, formatNumber, formatPrice } from "../../../shared/utils/format";
import { getCustomerStats, getCustomerSummary, searchOrders } from "../../orders/api";
import OrderStatusBadge from "../../orders/components/OrderStatusBadge";
import { getCustomers } from "../api";
import { customerStats, type CustomerStats } from "../analytics";
import StatCard from "../components/StatCard";
import ui from "../admin.module.css";
import styles from "./AdminCustomersPage.module.css";

const PAGE_SIZE = 20;
const SEARCH_DELAY_MS = 300;

async function loadOverview() {
  const [customers, summary] = await Promise.all([getCustomers({ pageSize: 1 }), getCustomerSummary()]);
  return { customerCount: customers.totalCount, ...summary };
}

// Liste Identity'den sayfalanır; sipariş özeti yalnızca bu sayfadaki müşteriler için Order'dan istenir
async function loadCustomerPage(q: string, page: number) {
  const users = await getCustomers({ q, page, pageSize: PAGE_SIZE });
  const stats = users.items.length > 0 ? await getCustomerStats(users.items.map((u) => u.id)) : [];
  return { customers: customerStats(users.items, stats), totalCount: users.totalCount };
}

const percentOf = (part: number, whole: number) => Math.round((part / whole) * 100);

export default function AdminCustomersPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [selected, setSelected] = useState<CustomerStats | null>(null);

  const query = searchParams.get("q") ?? "";
  const page = Math.max(1, Number(searchParams.get("page")) || 1);
  const debouncedQuery = useDebouncedValue(query, SEARCH_DELAY_MS);

  const overview = useAsync(loadOverview, []);
  const list = useAsync(() => loadCustomerPage(debouncedQuery, page), [debouncedQuery, page]);

  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(searchParams);
    if (value) next.set(key, value);
    else next.delete(key);
    if (key !== "page") next.delete("page");
    setSearchParams(next, { replace: true });
  };

  const search = useUrlSearchInput(query, (value) => setParam("q", value));

  const selectedId = selected?.user.id ?? null;
  const { data: selectedOrdersPage, isLoading: ordersLoading } = useAsync(
    () => (selectedId ? searchOrders({ buyerId: selectedId, pageSize: 20 }) : Promise.resolve(null)),
    [selectedId],
  );
  const selectedOrders = selectedOrdersPage?.items ?? [];

  const stats = overview.data;
  const pageCount = list.data ? Math.max(1, Math.ceil(list.data.totalCount / PAGE_SIZE)) : 1;

  return (
    <>
      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>Müşteriler</h1>
          <p className={ui.subtitle}>Kayıtlı müşteriler ve alışveriş geçmişleri.</p>
        </div>
      </div>

      {overview.isLoading ? (
        <div className={ui.stats} aria-busy="true" aria-label="Özet yükleniyor">
          {[1, 2, 3, 4].map((i) => (
            <Skeleton key={i} className={ui.skeletonStat} />
          ))}
        </div>
      ) : (
        stats && (
          <div className={ui.stats}>
            <StatCard icon={Users} label="Kayıtlı müşteri" value={formatNumber(stats.customerCount)} />
            <StatCard
              icon={UserPlus}
              label="Sipariş veren"
              value={formatNumber(stats.buyerCount)}
              hint={stats.customerCount > 0 ? `Müşterilerin %${percentOf(stats.buyerCount, stats.customerCount)}'i` : undefined}
            />
            <StatCard
              icon={Repeat}
              label="Tekrar alan"
              value={formatNumber(stats.repeatBuyerCount)}
              hint={stats.buyerCount > 0 ? `Alıcıların %${percentOf(stats.repeatBuyerCount, stats.buyerCount)}'i` : undefined}
            />
            <StatCard
              icon={Wallet}
              label="Müşteri başına harcama"
              value={formatPrice(stats.buyerCount > 0 ? stats.totalSpent / stats.buyerCount : 0)}
              hint="Sipariş veren müşteriler"
            />
          </div>
        )
      )}

      <div className={ui.toolbar}>
        <SearchInput
          className={ui.toolbarSearch}
          value={search.value}
          onChange={search.onChange}
          label="Müşterilerde ara"
          placeholder="Ad veya e-posta"
        />
      </div>

      {list.isLoading ? (
        <div aria-busy="true" aria-label="Müşteriler yükleniyor">
          {Array.from({ length: 6 }, (_, i) => (
            <Skeleton key={i} className={ui.skeletonRow} />
          ))}
        </div>
      ) : list.error || !list.data ? (
        <EmptyState
          icon={Users}
          title="Müşteriler yüklenemedi"
          text={getErrorMessage(list.error, "Identity veya Order servisine ulaşılamıyor.")}
          action={<Button onClick={list.reload}>Tekrar dene</Button>}
        />
      ) : list.data.customers.length === 0 && !debouncedQuery ? (
        <EmptyState icon={Users} title="Henüz müşteri yok" text="Kayıt olan müşteriler burada görünecek." />
      ) : list.data.customers.length === 0 ? (
        <EmptyState icon={SearchX} title="Eşleşen müşteri yok" text="Farklı bir ad veya e-posta ile aramayı deneyin." />
      ) : (
        <>
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
                {list.data.customers.map((customer) => (
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
          <div className={ui.tableFooter}>
            <span>
              {list.data.totalCount} müşteriden {(page - 1) * PAGE_SIZE + 1}–{(page - 1) * PAGE_SIZE + list.data.customers.length} arası
            </span>
            <Pagination page={page} pageCount={pageCount} onChange={(next) => setParam("page", next > 1 ? String(next) : null)} />
          </div>
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
            {ordersLoading ? (
              <div aria-busy="true" aria-label="Siparişler yükleniyor">
                {Array.from({ length: 3 }, (_, i) => (
                  <Skeleton key={i} className={ui.skeletonRow} />
                ))}
              </div>
            ) : selectedOrders.length === 0 ? (
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
