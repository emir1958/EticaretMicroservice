import type { LucideIcon } from "lucide-react";
import { ArrowDownRight, ArrowUpRight } from "lucide-react";
import ui from "../admin.module.css";
import styles from "./StatCard.module.css";

interface StatCardProps {
  label: string;
  value: string;
  icon?: LucideIcon;
  // Önceki döneme göre oransal değişim (0.12 = %12). null: kıyas yapılamıyor (önceki dönem 0)
  change?: number | null;
  // Artışın kötü olduğu metrikler (iptal oranı gibi) için renk ters çevrilir
  invert?: boolean;
  hint?: string;
}

const percent = (value: number) =>
  Math.abs(value).toLocaleString("tr-TR", { style: "percent", maximumFractionDigits: 1 });

export default function StatCard({ label, value, icon: Icon, change, invert = false, hint }: StatCardProps) {
  const isUp = change !== undefined && change !== null && change > 0;
  const isGood = change === 0 ? null : invert ? !isUp : isUp;

  return (
    <div className={ui.stat}>
      <span className={ui.statLabel}>
        {Icon && <Icon size={16} aria-hidden="true" />}
        {label}
      </span>
      <span className={ui.statValue}>{value}</span>
      {change !== undefined && (
        <span className={styles.change}>
          {change === null ? (
            <span className={styles.neutral}>Önceki dönemde veri yok</span>
          ) : (
            <>
              <span className={isGood === null ? styles.neutral : isGood ? styles.good : styles.bad}>
                {change !== 0 && (isUp ? <ArrowUpRight size={14} aria-hidden="true" /> : <ArrowDownRight size={14} aria-hidden="true" />)}
                <span className="visually-hidden">{isUp ? "artış" : change === 0 ? "değişim yok" : "azalış"}</span>
                {percent(change)}
              </span>
              <span className={styles.neutral}>önceki döneme göre</span>
            </>
          )}
        </span>
      )}
      {hint && <span className={ui.statHint}>{hint}</span>}
    </div>
  );
}
