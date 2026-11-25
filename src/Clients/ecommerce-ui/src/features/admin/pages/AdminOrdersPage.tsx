import { Link, useSearchParams } from "react-router-dom";
import { ChevronRight, SearchX, ShoppingCart } from "lucide-react";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import { useDebouncedValue } from "../../../shared/hooks/useDebouncedValue";
import { useUrlSearchInput } from "../../../shared/hooks/useUrlSearchInput";
import Badge from "../../../shared/components/Badge";
import Button from "../../../shared/components/Button";
import EmptyState from "../../../shared/components/EmptyState";
import FilterTabs from "../../../shared/components/FilterTabs";
import Pagination from "../../../shared/components/Pagination";
import SearchInput from "../../../shared/components/SearchInput";
import Skeleton from "../../../shared/components/Skeleton";
import { formatDate, formatPrice } from "../../../shared/utils/format";
import { searchOrders } from "../../orders/api";
import OrderStatusBadge, { PaymentStatusBadge } from "../../orders/components/OrderStatusBadge";
import type { OrderStatus } from "../../orders/types";
import { getUsers, type UserSummary } from "../api";
import { usersById } from "../analytics";
import ui from "../admin.module.css";

const PAGE_SIZE = 15;
const SEARCH_DELAY_MS = 300;

type StatusFilter = "all" | OrderStatus;

const STATUS_TABS: { value: StatusFilter; label: string }[] = [
  { value: "all", label: "Tümü" },
  { value: "Beklemede", label: "Bekleyen" },
  { value: "Tamamlandı", label: "Tamamlanan" },
  { value: "IptalEdildi", label: "İptal" },
];

export default function AdminOrdersPage() {
  // Arama, filtre ve sayfa adres çubuğunda: sipariş detayına gidip geri dönünce liste aynı yerde kalır
  const [searchParams, setSearchParams] = useSearchParams();

  const query = searchParams.get("q") ?? "";
  const status = (STATUS_TABS.find((t) => t.value === searchParams.get("status"))?.value ?? "all") as StatusFilter;
  const page = Math.max(1, Number(searchParams.get("page")) || 1);

  const debouncedQuery = useDebouncedValue(query, SEARCH_DELAY_MS);

  // Filtreleme, arama ve sayfalama sunucuda; tarayıcıya yalnızca bu sayfanın satırları gelir
  const { data, error, isLoading, reload } = useAsync(
    () =>
      searchOrders({
        q: debouncedQuery,
        status: status === "all" ? undefined : status,
        page,
        pageSize: PAGE_SIZE,
      }),
    [debouncedQuery, status, page],
  );

  // Müşteri adı/e-postası Identity'de; alınamazsa listede teslimat adresindeki isim gösterilir
  const { data: users } = useAsync(() => getUsers().catch(() => [] as UserSummary[]), []);
  const userMap = usersById(users ?? []);

  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(searchParams);
    if (value) next.set(key, value);
    else next.delete(key);
    if (key !== "page") next.delete("page");
    setSearchParams(next, { replace: true });
  };

  // Kutu yerel state'te (harf düşmez), URL anında güncellenir, sunucu isteği ise yazma durunca gider (debouncedQuery)
  const search = useUrlSearchInput(query, (value) => setParam("q", value));

  const counts = data?.statusCounts;
  const allCount = counts ? Object.values(counts).reduce((sum, n) => sum + n, 0) : undefined;
  const tabs = STATUS_TABS.map((tab) => ({ ...tab, count: tab.value === "all" ? allCount : counts?.[tab.value] }));

  const pageCount = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1;
  const hasFilters = Boolean(debouncedQuery) || status !== "all";

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
          value={search.value}
          onChange={search.onChange}
          label="Siparişlerde ara"
          placeholder="Sipariş no, alıcı adı veya il"
        />
        <FilterTabs label="Durum" tabs={tabs} value={status} onChange={(value) => setParam("status", value === "all" ? null : value)} />
      </div>

      {isLoading ? (
        <div aria-busy="true" aria-label="Siparişler yükleniyor">
          {Array.from({ length: 8 }, (_, i) => (
            <Skeleton key={i} className={ui.skeletonRow} />
          ))}
        </div>
      ) : error || !data ? (
        <EmptyState
          icon={ShoppingCart}
          title="Siparişler yüklenemedi"
          text={getErrorMessage(error, "Sipariş servisine ulaşılamıyor.")}
          action={<Button onClick={reload}>Tekrar dene</Button>}
        />
      ) : data.items.length === 0 && !hasFilters ? (
        <EmptyState icon={ShoppingCart} title="Henüz sipariş yok" text="Müşteriler sipariş verdiğinde burada görünecek." />
      ) : data.items.length === 0 ? (
        <EmptyState
          icon={SearchX}
          title="Eşleşen sipariş yok"
          text="Arama terimini veya durum filtresini değiştirmeyi deneyin."
          action={
            <Button
              variant="secondary"
              onClick={() => {
                search.clear();
                setSearchParams({}, { replace: true });
              }}
            >
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
                {data.items.map((order) => {
                  const user = userMap.get(order.buyerId);
                  return (
                    <tr key={order.id}>
                      <td className={ui.cellTitle}>#{order.id}</td>
                      <td>
                        <div className={ui.cellTitle}>{user?.username ?? order.contactName ?? "—"}</div>
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
                      <td className={ui.num}>{order.itemCount} adet</td>
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
              {data.totalCount} siparişten {(page - 1) * PAGE_SIZE + 1}–{(page - 1) * PAGE_SIZE + data.items.length} arası
            </span>
            <Pagination page={page} pageCount={pageCount} onChange={(next) => setParam("page", next > 1 ? String(next) : null)} />
          </div>
        </>
      )}
    </>
  );
}
