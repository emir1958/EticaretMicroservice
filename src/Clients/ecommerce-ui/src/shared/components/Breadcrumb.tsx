import { Link } from "react-router-dom";
import { ChevronRight } from "lucide-react";
import styles from "./Breadcrumb.module.css";

interface Crumb {
  label: string;
  // Son öğe (bulunulan sayfa) bağlantı değildir
  to?: string;
}

export default function Breadcrumb({ items }: { items: Crumb[] }) {
  return (
    <nav aria-label="Sayfa yolu">
      <ol className={styles.list}>
        {items.map((item, index) => (
          <li key={item.label} className={styles.item}>
            {index > 0 && <ChevronRight size={14} className={styles.separator} aria-hidden="true" />}
            {item.to ? (
              <Link to={item.to} className={styles.link}>
                {item.label}
              </Link>
            ) : (
              <span className={styles.current} aria-current="page">
                {item.label}
              </span>
            )}
          </li>
        ))}
      </ol>
    </nav>
  );
}
