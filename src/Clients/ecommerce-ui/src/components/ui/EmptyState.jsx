import styles from "./EmptyState.module.css";

// Boş liste / boş sepet gibi durumlar için ortak görünüm
export default function EmptyState({ icon: Icon, title, text, action }) {
  return (
    <div className={styles.empty}>
      {Icon && (
        <div className={styles.icon}>
          <Icon size={28} />
        </div>
      )}
      <h3 className={styles.title}>{title}</h3>
      {text && <p className={styles.text}>{text}</p>}
      {action && <div className={styles.action}>{action}</div>}
    </div>
  );
}
