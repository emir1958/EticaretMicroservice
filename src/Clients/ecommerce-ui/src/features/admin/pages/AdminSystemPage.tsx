import { Activity, Database, ExternalLink, FlaskConical, RefreshCw } from "lucide-react";
import { env } from "../../../config/env";
import { getErrorMessage } from "../../../lib/apiClient";
import { useAsync } from "../../../shared/hooks/useAsync";
import Alert from "../../../shared/components/Alert";
import Badge, { type BadgeTone } from "../../../shared/components/Badge";
import Button from "../../../shared/components/Button";
import Skeleton from "../../../shared/components/Skeleton";
import { formatDate, formatNumber } from "../../../shared/utils/format";
import { getProducts } from "../../catalog/api";
import { getAllOrders } from "../../orders/api";
import { getServiceHealth, getUsers, HEALTH_DASHBOARD_URL, type HealthStatus } from "../api";
import ui from "../admin.module.css";
import styles from "./AdminSystemPage.module.css";

const HEALTH_VIEW: Record<HealthStatus, { tone: BadgeTone; label: string }> = {
  Healthy: { tone: "success", label: "Çalışıyor" },
  Degraded: { tone: "warning", label: "Yavaş" },
  Unhealthy: { tone: "danger", label: "Erişilemiyor" },
};

const DEV_TOOLS = [
  { label: "Servis sağlık paneli", href: HEALTH_DASHBOARD_URL, text: "Gateway'in yokladığı tüm servisler" },
  { label: "Aspire dashboard", href: "http://localhost:18888", text: "Dağıtık trace ve loglar" },
  { label: "RabbitMQ yönetimi", href: "http://localhost:15672", text: "Kuyruklar ve hata kuyrukları" },
];

// Örnek veriyle gerçek veriyi ayırt etmek için: kaç kaydın seed'den geldiği
async function loadDataSummary() {
  const [products, users, orders] = await Promise.all([getProducts(), getUsers(), getAllOrders()]);
  const sampleUserIds = new Set(users.filter((u) => u.isSample).map((u) => u.id));
  return [
    { label: "Ürün", total: products.length, sample: products.filter((p) => p.isSample).length },
    { label: "Müşteri", total: users.filter((u) => u.role !== "Admin").length, sample: sampleUserIds.size },
    { label: "Sipariş", total: orders.length, sample: orders.filter((o) => sampleUserIds.has(o.buyerId)).length },
  ];
}

export default function AdminSystemPage() {
  const health = useAsync(getServiceHealth, []);
  const summary = useAsync(loadDataSummary, []);

  return (
    <>
      <div className={ui.header}>
        <div>
          <h1 className={ui.title}>Sistem durumu</h1>
          <p className={ui.subtitle}>Mikroservislerin sağlığı ve mağazadaki verinin kaynağı.</p>
        </div>
        <Button variant="secondary" onClick={health.reload} loading={health.isLoading}>
          <RefreshCw size={16} aria-hidden="true" /> Yenile
        </Button>
      </div>

      <section className={`${ui.panel} ${styles.section}`}>
        <h2 className={`${ui.panelTitle} ${styles.sectionTitle}`}>
          <Activity size={18} aria-hidden="true" /> Servisler
        </h2>
        {health.isLoading && !health.data ? (
          <div className={styles.services}>
            {[1, 2, 3, 4, 5].map((i) => (
              <Skeleton key={i} className={styles.skeleton} />
            ))}
          </div>
        ) : health.error ? (
          <Alert>
            {getErrorMessage(health.error, "Sağlık bilgisi alınamadı.")} Gateway çalışıyor mu? Ayrıntı için{" "}
            <a href={HEALTH_DASHBOARD_URL} target="_blank" rel="noreferrer" className={styles.inlineLink}>
              sağlık paneline
            </a>{" "}
            bakabilirsiniz.
          </Alert>
        ) : (
          <ul className={styles.services}>
            {(health.data ?? []).map((service) => {
              const view = HEALTH_VIEW[service.status] ?? HEALTH_VIEW.Unhealthy;
              return (
                <li key={service.name} className={styles.service}>
                  <div className={styles.serviceHead}>
                    <span className={ui.cellTitle}>{service.name.replace(" Microservice", "")}</span>
                    <Badge tone={view.tone} dot>
                      {view.label}
                    </Badge>
                  </div>
                  <ul className={styles.entries}>
                    {service.entries.map((entry) => (
                      <li key={entry.name}>
                        <Database size={14} aria-hidden="true" />
                        <span>{entry.name}</span>
                        <span className={entry.status === "Healthy" ? styles.ok : styles.fail}>
                          {HEALTH_VIEW[entry.status]?.label ?? entry.status}
                        </span>
                      </li>
                    ))}
                  </ul>
                  <span className={ui.cellSub}>Son kontrol: {formatDate(service.lastExecuted)}</span>
                </li>
              );
            })}
          </ul>
        )}
      </section>

      <div className={styles.grid}>
        <section className={ui.panel}>
          <h2 className={`${ui.panelTitle} ${styles.sectionTitle}`}>
            <FlaskConical size={18} aria-hidden="true" /> Veri kaynağı
          </h2>
          {summary.isLoading ? (
            <Skeleton className={styles.skeleton} />
          ) : summary.error || !summary.data ? (
            <Alert>{getErrorMessage(summary.error, "Veri özeti alınamadı.")}</Alert>
          ) : (
            <>
              <table className={ui.table}>
                <thead>
                  <tr>
                    <th>Kayıt</th>
                    <th className={ui.alignRight}>Toplam</th>
                    <th className={ui.alignRight}>Örnek</th>
                    <th className={ui.alignRight}>Gerçek</th>
                  </tr>
                </thead>
                <tbody>
                  {summary.data.map((row) => (
                    <tr key={row.label}>
                      <td className={ui.cellTitle}>{row.label}</td>
                      <td className={`${ui.alignRight} ${ui.num}`}>{formatNumber(row.total)}</td>
                      <td className={`${ui.alignRight} ${ui.num} ${ui.muted}`}>{formatNumber(row.sample)}</td>
                      <td className={`${ui.alignRight} ${ui.num}`}>{formatNumber(row.total - row.sample)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <p className={styles.note}>
                Örnek veri, servisler <code>SEED_SAMPLE_DATA=true</code> ile başlatıldığında bir kez eklenir ve mevcut
                kayıtlara dokunmaz. Örnek müşterilerin e-postaları <code>@ornek.test</code> ile biter ve giriş yapamaz;
                örnek siparişler stok ve ödeme servislerinden geçmez.
              </p>
            </>
          )}
        </section>

        {env.isDev && (
          <section className={ui.panel}>
            <h2 className={`${ui.panelTitle} ${styles.sectionTitle}`}>
              <ExternalLink size={18} aria-hidden="true" /> Geliştirici araçları
            </h2>
            <ul className={styles.tools}>
              {DEV_TOOLS.map((tool) => (
                <li key={tool.href}>
                  <a href={tool.href} target="_blank" rel="noreferrer" className={styles.tool}>
                    <span>
                      <span className={ui.cellTitle}>{tool.label}</span>
                      <span className={ui.cellSub}>{tool.text}</span>
                    </span>
                    <ExternalLink size={16} aria-hidden="true" />
                  </a>
                </li>
              ))}
            </ul>
          </section>
        )}
      </div>
    </>
  );
}
