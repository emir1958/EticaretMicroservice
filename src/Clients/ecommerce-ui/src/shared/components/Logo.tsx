import { brand } from "../../config/brand";
import styles from "./Logo.module.css";

// Marka işareti: üç satır, ortadaki kısa ve vurgulu ("düzen").
export default function Logo({ tone = "dark" }: { tone?: "dark" | "light" }) {
  return (
    <span className={`${styles.logo} ${styles[tone]}`}>
      <svg className={styles.mark} viewBox="0 0 32 32" aria-hidden="true">
        <rect className={styles.markBg} width="32" height="32" rx="8" />
        <rect className={styles.markLine} x="8" y="9" width="16" height="3" rx="1.5" />
        <rect className={styles.markAccent} x="8" y="14.5" width="11" height="3" rx="1.5" />
        <rect className={styles.markLine} x="8" y="20" width="16" height="3" rx="1.5" />
      </svg>
      <span className={styles.name}>{brand.name}</span>
    </span>
  );
}
