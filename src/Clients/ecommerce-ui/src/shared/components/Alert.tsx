import type { ReactNode } from "react";
import { AlertTriangle, CheckCircle } from "lucide-react";
import styles from "./Alert.module.css";

interface AlertProps {
  tone?: "danger" | "success";
  className?: string;
  children: ReactNode;
}

const ICONS = { danger: AlertTriangle, success: CheckCircle };

// Hata "alert" (ekran okuyucu hemen okur), başarı "status" (sırası gelince okur)
export default function Alert({ tone = "danger", className, children }: AlertProps) {
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
