import { formatDate } from "../../../shared/utils/format";
import { HISTORY_VIEW } from "../orderStatus";
import type { OrderHistoryEntry } from "../types";
import styles from "./OrderTimeline.module.css";

// Kayıtlar API'den zaman sırasıyla gelir
export default function OrderTimeline({ entries }: { entries: OrderHistoryEntry[] }) {
  return (
    <ol className={styles.timeline}>
      {entries.map((entry, index) => {
        const view = HISTORY_VIEW[entry.type];
        return (
          <li key={index} className={`${styles.entry} ${styles[view.tone]}`}>
            <span className={styles.dot} aria-hidden="true" />
            <div>
              <div className={styles.label}>{view.label}</div>
              <time className={styles.time} dateTime={entry.occurredAt}>
                {formatDate(entry.occurredAt)}
              </time>
              {entry.note && <p className={styles.note}>{entry.note}</p>}
            </div>
          </li>
        );
      })}
    </ol>
  );
}
