import { AlertTriangle, CheckCircle } from "lucide-react";
import styles from "./Alert.module.css";

const ICONS = { danger: AlertTriangle, success: CheckCircle };

// Hata veya başarı bildirimi. tone: danger | success
// role: hata "alert" (ekran okuyucu hemen okur), başarı "status" (sırası gelince okur).
export default function Alert({ tone = "danger", className, children }) {
  const Icon = ICONS[tone];
  return (
    <div
      role={tone === "danger" ? "alert" : "status"}
      className={[styles.alert, styles[tone], className].filter(Boolean).join(" ")}
    >
      <Icon size={18} className={styles.icon} aria-hidden="true" />
      <div>{children}</div>
    </div>
  );
}
