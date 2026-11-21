import { Link, useSearchParams } from "react-router-dom";
import { ChevronRight, SearchX, ShoppingCart } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import Badge from "../../../shared/components/Badge";
import Button from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import FilterTabs from "../../../shared/components/FilterTabs";
import Pagination from "../../../shared/components/Pagination";
import SearchInput from "../../../shared/components/SearchInput";
import Skeleton from "../../../shared/components/Skeleton";
import { formatDate, formatPrice } from "../../../shared/utils/format";
import { normalizeText } from "../../catalog/catalogView";
import { getAllOrders } from "../../orders/api";
import OrderStatusBadge, { PaymentStatusBadge } from "../../orders/components/OrderStatusBadge";
import type { Order, OrderStatus } from "../../orders/types";
import { getUsers, type UserSummary } from "../api";
import { itemCountOf, usersById } from "../analytics";
import ui from "../admin.module.css";

const PAGE_SIZE = 15;

type StatusFilter = "all" | OrderStatus;

const STATUS_TABS: { value: StatusFilter; label: string }[] = [
  { value: "all", label: "Tümü" },
  { value: "Beklemede", label: "Bekleyen" },
  { value: "Tamamlandı", label: "Tamamlanan" },
  { value: "IptalEdildi", label: "İptal" },
];

async function loadOrders() {
  const [orders, users] = await Promise.all([getAllOrders(), getUsers().catch(() => [] as UserSummary[])]);
  return { orders, users };
}

// Siparişi müşteri adı, e-postası, sipariş numarası veya teslimat ilinden bulur
function matches(order: Order, user: UserSummary | undefined, terms: string[]) {
  const haystack = normalizeText(
    [`#${order.id}`, order.id, user?.username, user?.email, order.address?.contactName, order.address?.city].join(" "),
  );
  return terms.every((term) => haystack.includes(term));
}

export default function AdminOrdersPage() {
  const { data, error, isLoading, reload } = useAsync(loadOrders, []);
  // Arama ve filtre adres çubuğunda: sipariş detayına gidip geri dönünce liste aynı yerde kalır
  const [searchParams, setSearchParams] = useSearchParams();

  const query = searchParams.get("q") ?? "";
  const status = (STATUS_TABS.find((t) => t.value === searchParams.get("status"))?.value ?? "all") as StatusFilter;

  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(searchParams);
    if (value) next.set(key, value);
    else next.delete(key);
    if (key !== "page") next.delete("page");
    setSearchParams(next, { replace: true });
  };

  const orders = data?.orders ?? [];
  const userMap = usersById(data?.users ?? []);
  const terms = normalizeText(query).split(/\s+/).filter(Boolean);
  const searched = orders.filter((order) => matches(order, userMap.get(order.buyerId), terms));
  const visible = status === "all" ? searched : searched.filter((order) => order.orderStatus === status);

  const pageCount = Math.max(1, Math.ceil(visible.length / PAGE_SIZE));
  const page = Math.min(Math.max(1, Number(searchParams.get("page")) || 1), pageCount);
  const pageItems = visible.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  const tabs = STATUS_TABS.map((tab) => ({
    ...tab,
    count: tab.value === "all" ? searched.length : searched.filter((o) => o.orderStatus === tab.value).length,
  }));

  return (
    <>
      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>Siparişler</h1>
          <p className={ui.subtitle}>Mağazaya gelen tüm siparişler; durumlar sipariş saga'sı tarafından güncellenir.</p>
        </div>
      </div>

      <div className={ui.toolbar}>
        <SearchInput
          className={ui.toolbarSearch}
          value={query}
          onChange={(value) => setParam("q", value || null)}
          label="Siparişlerde ara"
          placeholder="Sipariş no, müşteri veya il"
        />
        <FilterTabs label="Durum" tabs={tabs} value={status} onChange={(value) => setParam("status", value === "all" ? null : value)} />
      </div>

      {isLoading ? (
        <div aria-busy="true" aria-label="Siparişler yükleniyor">
          {Array.from({ length: 8 }, (_, i) => (
            <Skeleton key={i} className={ui.skeletonRow} />
          ))}
        </div>
      ) : error ? (
        <EmptyState
          icon={ShoppingCart}
          title="Siparişler yüklenemedi"
          text={getErrorMessage(error, "Sipariş servisine ulaşılamıyor.")}
          action={<Button onClick={reload}>Tekrar dene</Button>}
        />
      ) : orders.length === 0 ? (
        <EmptyState icon={ShoppingCart} title="Henüz sipariş yok" text="Müşteriler sipariş verdiğinde burada görünecek." />
      ) : visible.length === 0 ? (
        <EmptyState
          icon={SearchX}
          title="Eşleşen sipariş yok"
          text="Arama terimini veya durum filtresini değiştirmeyi deneyin."
          action={
            <Button variant="secondary" onClick={() => setSearchParams({}, { replace: true })}>
              Filtreleri temizle
            </Button>
          }
        />
      ) : (
        <>
          <div className={ui.tableWrap}>
            <table className={ui.table}>
              <thead>
                <tr>
                  <th>Sipariş</th>
                  <th>Müşteri</th>
                  <th>Tarih</th>
                  <th>Ürün</th>
                  <th>Ödeme</th>
                  <th>Durum</th>
                  <th className={ui.alignRight}>Tutar</th>
                  <th>
                    <span className="visually-hidden">İşlem</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {pageItems.map((order) => {
                  const user = userMap.get(order.buyerId);
                  return (
                    <tr key={order.id}>
                      <td className={ui.cellTitle}>#{order.id}</td>
                      <td>
                        {/* Müşteri adı Identity'de; listesi alınamazsa teslimat adresindeki isim gösterilir */}
                        <div className={ui.cellTitle}>{user?.username ?? order.address?.contactName ?? "—"}</div>
                        <div className={ui.cellSub}>
                          {user?.email ?? <span className={ui.mono}>{order.buyerId.slice(0, 8)}…</span>}
                          {user?.isSample && (
                            <>
                              {" "}
                              <Badge>Örnek</Badge>
                            </>
                          )}
                        </div>
                      </td>
                      <td className={`${ui.muted} ${ui.nowrap}`}>{formatDate(order.createdDate)}</td>
                      <td className={ui.num}>{itemCountOf(order)} adet</td>
                      <td>
                        <PaymentStatusBadge status={order.paymentStatus} />
                      </td>
                      <td>
                        <OrderStatusBadge status={order.orderStatus} />
                      </td>
                      <td className={`${ui.alignRight} ${ui.num} ${ui.strong}`}>{formatPrice(order.totalPrice)}</td>
                      <td className={ui.alignRight}>
                        <Link to={`/admin/orders/${order.id}`} className={ui.rowLink} aria-label={`Sipariş #${order.id} detayı`}>
                          Detay <ChevronRight size={16} aria-hidden="true" />
                        </Link>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <div className={ui.tableFooter}>
            <span>
              {visible.length} siparişten {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, visible.length)} arası
            </span>
            <Pagination page={page} pageCount={pageCount} onChange={(next) => setParam("page", next > 1 ? String(next) : null)} />
          </div>
        </>
      )}
    </>
  );
}
