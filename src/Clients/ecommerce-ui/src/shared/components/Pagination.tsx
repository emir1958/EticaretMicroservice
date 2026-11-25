import { ChevronLeft, ChevronRight } from "lucide-react";
import styles from "./Pagination.module.css";

interface PaginationProps {
  page: number;
  pageCount: number;
  onChange: (page: number) => void;
}

// 1 … 4 5 6 … 12 biçiminde: ilk, son ve geçerli sayfanın komşuları
function visiblePages(page: number, pageCount: number): (number | "gap")[] {
  const pages = new Set([1, pageCount, page - 1, page, page + 1]);
  const sorted = [...pages].filter((p) => p >= 1 && p <= pageCount).sort((a, b) => a - b);

  const result: (number | "gap")[] = [];
  sorted.forEach((p, i) => {
    if (i > 0 && p - sorted[i - 1] > 1) result.push("gap");
    result.push(p);
  });
  return result;
}

export default function Pagination({ page, pageCount, onChange }: PaginationProps) {
  if (pageCount <= 1) return null;

  return (
    <nav className={styles.pagination} aria-label="Sayfalar">
      <button type="button" className={styles.page} disabled={page <= 1} onClick={() => onChange(page - 1)} aria-label="Önceki sayfa">
        <ChevronLeft size={16} aria-hidden="true" />
      </button>
      {visiblePages(page, pageCount).map((item, index) =>
        item === "gap" ? (
          <span key={`gap-${index}`} className={styles.gap} aria-hidden="true">
            …
          </span>
        ) : (
          <button
            key={item}
            type="button"
            className={`${styles.page} ${item === page ? styles.current : ""}`}
            aria-current={item === page ? "page" : undefined}
            aria-label={`Sayfa ${item}`}
            onClick={() => onChange(item)}
          >
            {item}
          </button>
        ),
      )}
      <button
        type="button"
        className={styles.page}
        disabled={page >= pageCount}
        onClick={() => onChange(page + 1)}
        aria-label="Sonraki sayfa"
      >
        <ChevronRight size={16} aria-hidden="true" />
      </button>
    </nav>
  );
}
