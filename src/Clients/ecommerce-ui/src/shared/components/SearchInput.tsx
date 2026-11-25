import type { ComponentProps } from "react";
import { Search, X } from "lucide-react";
import styles from "./SearchInput.module.css";

interface SearchInputProps extends Omit<ComponentProps<"input">, "value" | "onChange" | "type" | "className"> {
  value: string;
  onChange: (value: string) => void;
  // Görünür etiket yok: alanın adı ekran okuyucuya bununla verilir
  label: string;
  className?: string;
}

export default function SearchInput({ value, onChange, label, className, ...inputProps }: SearchInputProps) {
  return (
    <div className={[styles.wrap, className].filter(Boolean).join(" ")}>
      <Search size={18} className={styles.icon} aria-hidden="true" />
      <input
        type="search"
        className={styles.input}
        aria-label={label}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        {...inputProps}
      />
      {value && (
        <button type="button" className={styles.clear} onClick={() => onChange("")} aria-label="Aramayı temizle">
          <X size={16} aria-hidden="true" />
        </button>
      )}
    </div>
  );
}
