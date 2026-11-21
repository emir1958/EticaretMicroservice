import styles from "./FilterTabs.module.css";

interface FilterTab<T extends string> {
  value: T;
  label: string;
  count?: number;
}

interface FilterTabsProps<T extends string> {
  label: string;
  tabs: FilterTab<T>[];
  value: T;
  onChange: (value: T) => void;
}

// Bir listeyi süzen seçenekler (Tümü / Tamamlanan / İptal). İçerik paneli değiştirmediği için
// "tab" değil, basılı durumu olan buton grubudur (aria-pressed).
export default function FilterTabs<T extends string>({ label, tabs, value, onChange }: FilterTabsProps<T>) {
  return (
    <div className={styles.tabs} role="group" aria-label={label}>
      {tabs.map((tab) => (
        <button
          key={tab.value}
          type="button"
          className={`${styles.tab} ${tab.value === value ? styles.active : ""}`}
          aria-pressed={tab.value === value}
          onClick={() => onChange(tab.value)}
        >
          {tab.label}
          {tab.count !== undefined && <span className={styles.count}>{tab.count}</span>}
        </button>
      ))}
    </div>
  );
}
