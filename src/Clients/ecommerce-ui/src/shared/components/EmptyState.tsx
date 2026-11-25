import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import styles from "./EmptyState.module.css";

interface EmptyStateProps {
  icon?: LucideIcon;
  title: string;
  text?: ReactNode;
  action?: ReactNode;
  // plain: çerçevesiz (panel veya çekmece içinde kullanılırken)
  variant?: "card" | "plain";
}

export default function EmptyState({ icon: Icon, title, text, action, variant = "card" }: EmptyStateProps) {
  return (
    <div className={`${styles.empty} ${styles[variant]}`}>
      {Icon && (
        <div className={styles.icon}>
          <Icon size={26} aria-hidden="true" />
        </div>
      )}
      <h3 className={styles.title}>{title}</h3>
      {text && <p className={styles.text}>{text}</p>}
      {action && <div className={styles.action}>{action}</div>}
    </div>
  );
}
