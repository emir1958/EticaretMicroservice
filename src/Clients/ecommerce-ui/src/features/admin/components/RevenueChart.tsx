import { useState } from "react";
import { formatCompactPrice, formatPrice } from "../../../shared/utils/format";
import type { DailyPoint } from "../analytics";
import styles from "./RevenueChart.module.css";

const HEIGHT = 100;
const BAR_SLOT = 10;
const BAR_GAP = 2;

const dayLabel = (date: Date) => date.toLocaleDateString("tr-TR", { day: "numeric", month: "short" });

// Günlük ciro sütun grafiği. SVG viewBox'ı genişliğe göre esner; yazılar SVG'nin dışında HTML olarak
// durur ki esnemesin. Üzerine gelinen gün başlıkta gösterilir.
// Ekran okuyucu için aynı veri gizli bir tablo olarak da verilir.
export default function RevenueChart({ points }: { points: DailyPoint[] }) {
  const [activeIndex, setActiveIndex] = useState<number | null>(null);

  const max = Math.max(...points.map((p) => p.revenue), 1);
  const total = points.reduce((sum, p) => sum + p.revenue, 0);
  const active = activeIndex !== null ? points[activeIndex] : null;
  const width = points.length * BAR_SLOT;

  const labelIndexes = [0, Math.floor((points.length - 1) / 2), points.length - 1];

  return (
    <figure className={styles.figure}>
      <figcaption className={styles.readout} aria-live="polite">
        <span className={styles.readoutLabel}>{active ? dayLabel(active.date) : `Son ${points.length} gün`}</span>
        <span className={styles.readoutValue}>{formatPrice(active ? active.revenue : total)}</span>
        {active && <span className={styles.readoutSub}>{active.orders} sipariş</span>}
      </figcaption>

      <div className={styles.plot}>
        <div className={styles.yAxis} aria-hidden="true">
          <span>{formatCompactPrice(max)}</span>
          <span>{formatCompactPrice(max / 2)}</span>
          <span>₺0</span>
        </div>

        <svg
          className={styles.svg}
          viewBox={`0 0 ${width} ${HEIGHT}`}
          preserveAspectRatio="none"
          aria-hidden="true"
          onMouseLeave={() => setActiveIndex(null)}
        >
          {[0, 0.5, 1].map((ratio) => (
            <line key={ratio} className={styles.grid} x1={0} x2={width} y1={HEIGHT * ratio} y2={HEIGHT * ratio} />
          ))}
          {points.map((point, index) => {
            const barHeight = (point.revenue / max) * (HEIGHT - 4);
            return (
              <g key={point.date.getTime()} onMouseEnter={() => setActiveIndex(index)}>
                {/* Hover alanı sütunun tamamıdır: kısa çubuklara da kolay gelinir */}
                <rect className={styles.hit} x={index * BAR_SLOT} y={0} width={BAR_SLOT} height={HEIGHT} />
                <rect
                  className={`${styles.bar} ${activeIndex === index ? styles.barActive : ""}`}
                  x={index * BAR_SLOT + BAR_GAP / 2}
                  y={HEIGHT - barHeight}
                  width={BAR_SLOT - BAR_GAP}
                  height={Math.max(barHeight, point.orders > 0 ? 1 : 0)}
                  rx={1}
                />
              </g>
            );
          })}
        </svg>
      </div>

      <div className={styles.xAxis} aria-hidden="true">
        {labelIndexes.map((index) => (
          <span key={index}>{points[index] && dayLabel(points[index].date)}</span>
        ))}
      </div>

      <table className="visually-hidden">
        <caption>Günlük ciro</caption>
        <thead>
          <tr>
            <th>Gün</th>
            <th>Ciro</th>
            <th>Sipariş</th>
          </tr>
        </thead>
        <tbody>
          {points.map((point) => (
            <tr key={point.date.getTime()}>
              <td>{dayLabel(point.date)}</td>
              <td>{formatPrice(point.revenue)}</td>
              <td>{point.orders}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </figure>
  );
}
