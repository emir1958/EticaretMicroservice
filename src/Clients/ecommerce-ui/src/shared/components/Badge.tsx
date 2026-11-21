import type { ReactNode } from "react";
import styles from "./Badge.module.css";

export type BadgeTone = "neutral" | "success" | "warning" | "danger" | "info" | "accent" | "solid";

interface BadgeProps {
  tone?: BadgeTone;
  // Durum rozeti: metnin önünde renkli nokta. Renk tek başına anlam taşımaz, metin her zaman vardır.
  dot?: boolean;
  className?: string;
  children: ReactNode;
}

export default function Badge({ tone = "neutral", dot = false, className, children }: BadgeProps) {
  return (
    <span className={[styles.badge, styles[tone], className].filter(Boolean).join(" ")}>
      {dot && <span className={styles.dot} aria-hidden="true" />}
      {children}
    </span>
  );
}
