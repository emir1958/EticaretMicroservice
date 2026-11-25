import styles from "./PageLoader.module.css";

// Ayrı parça olarak yüklenen (lazy) sayfa indirilirken gösterilir
export default function PageLoader() {
  return (
    <div className={styles.loader} role="status">
      <span className={styles.spinner} aria-hidden="true" />
      <span className="visually-hidden">Sayfa yükleniyor</span>
    </div>
  );
}
